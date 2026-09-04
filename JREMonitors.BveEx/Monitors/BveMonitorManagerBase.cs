using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Builders.Base;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using SlimDX.Direct3D9;
using Monitor = JREMonitors.Core.Monitors.Monitor;
using Vector2 = System.Numerics.Vector2;

namespace JREMonitors.BveEx.Monitors
{
    public abstract class BveMonitorManagerBase : MonitorManager
    {
        protected const float MaxMoveRange = 5;

        private readonly Stopwatch _drawMonitorsSw = new Stopwatch();

        private readonly PendingClickQueue<MonitorHolderBase> _pendingClickQueue =
            new PendingClickQueue<MonitorHolderBase>();

        private readonly Stopwatch _renderCabSw = new Stopwatch();
        private readonly Stopwatch _renderExternalSw = new Stopwatch();
        private readonly Stopwatch _submitFrameSw = new Stopwatch();
        private readonly Stopwatch _syncFrameSw = new Stopwatch();
        private readonly Stopwatch _syncSw = new Stopwatch();
        protected readonly Device D3D9Device;
        protected readonly IDebugger Debugger;
        protected readonly Form MainForm;
        protected readonly List<MonitorHolderBase> MonitorHolders;
        protected readonly ITimeProvider TimeProvider;
        protected readonly BveWindowHook WindowHook;
        protected int BufferFrameCount;
        protected bool ContentDrawnThisFrame;
        protected Scenario CurrentScenario;
        protected bool HasMouseDownDuringTick;
        protected bool HasRendered;
        protected bool IsMouseDown;
        protected Vector2 MouseDownPos;
        protected bool ShowTextureBoundsRect;

        protected BveMonitorManagerBase(
            DataHub dataHub,
            MonitorContext context,
            IEnumerable<MonitorProperties> monitorProperties,
            ITimeProvider timeProvider,
            bool showTextureBoundsRect,
            int rawBufferFrameCount
        ) : base(context, dataHub)
        {
            Debugger = dataHub.GetOrNull<IDebugger>();
            var bveHacker = dataHub.Get<IBveHacker>();
            MainForm = bveHacker.MainFormSource;
            D3D9Device = Direct3DProvider.Instance.Device;
            WindowHook = new BveWindowHook(D3D9Device.CreationParameters.Window);
            WindowHook.AfterResize += OnAfterResize;
            TimeProvider = timeProvider;
            ShowTextureBoundsRect = showTextureBoundsRect;
            BufferFrameCount = ClampBufferFrameCount(rawBufferFrameCount);
            MonitorHolders = CreateHolders(monitorProperties, timeProvider, showTextureBoundsRect, BufferFrameCount,
                OnExternalClick);
            Monitors = MonitorHolders.ToDictionary(holder => holder.Monitor.Id, holder => holder.Monitor);
            MainForm.MouseDown += OnMouseDown;
            MainForm.MouseMove += OnMouseMove;
            MainForm.MouseUp += OnMouseUp;
        }

        public IReadOnlyList<MonitorHolderBase> Holders => MonitorHolders;

        public Dictionary<string, Monitor> Monitors { get; }

        protected abstract MonitorHolderBase CreateHolder(
            MonitorProperties config,
            ITimeProvider timeProvider,
            bool showTextureBoundsRect,
            int bufferFrameCount,
            Action<MonitorHolderBase, Vector2> externalClickCallback
        );

        protected List<MonitorHolderBase> CreateHolders(
            IEnumerable<MonitorProperties> monitorProperties,
            ITimeProvider timeProvider,
            bool showTextureBoundsRect,
            int bufferFrameCount,
            Action<MonitorHolderBase, Vector2> externalClickCallback
        )
        {
            return monitorProperties
                .Select(config => CreateHolder(config, timeProvider, showTextureBoundsRect, bufferFrameCount,
                    externalClickCallback))
                .ToList();
        }


        public void DrawMonitors(TimeSpan elapsed)
        {
            if (!HasRendered) HasRendered = true;
            HasMouseDownDuringTick = false;
            ProcessPendingClick();
            var status = D3D9Device.TestCooperativeLevel();
            if (status.IsFailure)
            {
                CleanupDeviceResources();
                return;
            }

            _drawMonitorsSw.Restart();
            ContentDrawnThisFrame = true;
            Context.D2D1Context.BeginDraw();
            for (var i = 0; i < MonitorHolders.Count; i++) MonitorHolders[i].DrawMonitorContent(elapsed);

            Context.D2D1Context.Target = null;
            _drawMonitorsSw.Stop();
            Debugger?.AddLine($"drawMonitors elapsed:{_drawMonitorsSw.Elapsed.TotalMilliseconds} ms");
        }

        public void SyncFrame()
        {
            var status = D3D9Device.TestCooperativeLevel();
            if (status.IsFailure)
            {
                CleanupDeviceResources();
                return;
            }

            _syncFrameSw.Restart();
            _renderCabSw.Restart();
            if (!ContentDrawnThisFrame) Context.D2D1Context.BeginDraw();

            for (var i = 0; i < MonitorHolders.Count; i++)
                MonitorHolders[i].RenderCabProjectionFrame(!ContentDrawnThisFrame);

            Context.D2D1Context.EndDraw();
            Context.D2D1Context.Target = null;
            _renderCabSw.Stop();
            _submitFrameSw.Restart();
            for (var i = 0; i < MonitorHolders.Count; i++)
            {
                var holder = MonitorHolders[i];
                holder.Submit();
            }

            Context.D3D11Context.Flush();
            _submitFrameSw.Stop();

            _syncSw.Restart();
            for (var i = 0; i < MonitorHolders.Count; i++)
            {
                var holder = MonitorHolders[i];
                holder.Sync();
            }

            _syncSw.Stop();
            _renderExternalSw.Restart();
            for (var i = 0; i < MonitorHolders.Count; i++) MonitorHolders[i].RenderExternal();

            _renderExternalSw.Stop();
            _syncFrameSw.Stop();
            Debugger?.AddLine($"renderCab elapsed:{_renderCabSw.Elapsed.TotalMilliseconds} ms");
            Debugger?.AddLine($"submit elapsed:{_submitFrameSw.Elapsed.TotalMilliseconds} ms");
            Debugger?.AddLine($"sync elapsed:{_syncSw.Elapsed.TotalMilliseconds} ms");
            Debugger?.AddLine($"renderExternal elapsed:{_renderExternalSw.Elapsed.TotalMilliseconds} ms");
            Debugger?.AddLine($"syncFrame elapsed:{_syncFrameSw.Elapsed.TotalMilliseconds} ms");
            ContentDrawnThisFrame = false;
        }

        private void ProcessPendingClick()
        {
            if (!_pendingClickQueue.TryDequeue(out var holder, out var pos)) return;
            // 外屏
            if (holder != null)
            {
                holder.Monitor.Click(holder.MonitorGameOutput.Size, pos);
                return;
            }

            // 游戏内
            var clientSize = MainForm.ClientSize;
            var panelPos = ProjectionHelper.ProjectMouseClickToPanel(
                CurrentScenario.Vehicle, pos.X, pos.Y, clientSize.Width, clientSize.Height
            );
            foreach (var h in MonitorHolders)
                if (h.TryProjectPanelToMonitor(panelPos, out var finalPos))
                {
                    h.Monitor.Click(h.MonitorGameOutput.Size, finalPos);
                    break;
                }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (CurrentScenario == null || !HasRendered || HasMouseDownDuringTick ||
                e.Button != MouseButtons.Left) return;
            IsMouseDown = true;
            MouseDownPos = new Vector2(e.X, e.Y);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown) return;
            var currentPos = new Vector2(e.X, e.Y);
            if ((currentPos - MouseDownPos).LengthSquared() > MaxMoveRange) IsMouseDown = false;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown) return;
            IsMouseDown = false;
            HasMouseDownDuringTick = true;
            _pendingClickQueue.TryEnqueue(null, new Vector2(e.X, e.Y));
        }

        private void OnExternalClick(MonitorHolderBase holder, Vector2 mappedPos)
        {
            _pendingClickQueue.TryEnqueue(holder, mappedPos);
        }

        private void OnAfterResize(object sender, EventArgs e)
        {
            RestoreDeviceResources();
        }

        public void Initialize(Scenario scenario)
        {
            CurrentScenario = scenario;
            foreach (var holder in MonitorHolders) holder.Initialize(scenario);
        }

        public void AttachMonitors()
        {
            foreach (var holder in MonitorHolders) holder.Attach();
        }

        public void DetachMonitors()
        {
            foreach (var holder in MonitorHolders) holder.Detach();
            HasRendered = IsMouseDown = HasMouseDownDuringTick = false;
        }

        public void OnDeviceLost()
        {
            CleanupDeviceResources();
        }

        protected virtual void CleanupDeviceResources()
        {
            foreach (var h in MonitorHolders) h.DisposeD3D9Texture();
        }

        private void RestoreDeviceResources()
        {
            foreach (var h in MonitorHolders)
            {
                h.EnsureD3D9Texture();
                h.Sync();
            }
        }

        public void Reconfigure(IVehicleBuilder builder, VehicleBuildContext context, VehicleConfig oldConfig,
            VehicleConfig newConfig)
        {
            var newProps = builder.CreateMonitorProperties(Context, newConfig, DataHub);
            var newPropsById = newProps.ToDictionary(p => p.Id);
            var existingIds = new HashSet<string>(MonitorHolders.Select(h => h.Monitor.Id));
            var removedIds = existingIds.Except(newPropsById.Keys).ToList();
            foreach (var id in removedIds)
            {
                var holder = MonitorHolders.First(h => h.Monitor.Id == id);
                builder.OnMonitorsRemoved(context, new[] { holder.Monitor }, newConfig);
                holder.Detach();
                holder.Dispose();
                MonitorHolders.Remove(holder);
            }

            var addedProps = newPropsById.Where(p => !existingIds.Contains(p.Key)).Select(p => p.Value).ToList();
            var addedHolders = new List<MonitorHolderBase>();
            if (addedProps.Count > 0)
            {
                var addedMonitors = new List<Monitor>();
                foreach (var p in addedProps)
                {
                    var holder = CreateHolder(p, TimeProvider, ShowTextureBoundsRect, BufferFrameCount,
                        OnExternalClick);
                    MonitorHolders.Add(holder);
                    addedHolders.Add(holder);
                    addedMonitors.Add(holder.Monitor);
                }

                builder.PopulateMonitorLocalDataHub(context, addedMonitors, newConfig);
                if (CurrentScenario != null)
                    foreach (var h in addedHolders)
                    {
                        h.Initialize(CurrentScenario);
                        h.Attach();
                    }
            }

            foreach (var id in existingIds.Intersect(newPropsById.Keys))
            {
                var holder = MonitorHolders.First(h => h.Monitor.Id == id);
                var newP = newPropsById[id];
                if (newP.Size != holder.Properties.Size)
                    holder.ReconfigureResolution(newP);
                else
                    holder.ReconfigureCabProjection(newP);
            }

            Monitors.Clear();
            foreach (var h in MonitorHolders) Monitors[h.Monitor.Id] = h.Monitor;
            builder.ReconfigureMonitorLocal(context, oldConfig, newConfig, Monitors.Values.ToList());
        }

        public void SetShowTextureBoundsRect(bool value)
        {
            ShowTextureBoundsRect = value;
            for (var i = 0; i < MonitorHolders.Count; i++)
                MonitorHolders[i].SetShowTextureBoundsRect(value);
        }

        public void SetShowUiDebugRect(bool value)
        {
            for (var i = 0; i < MonitorHolders.Count; i++)
                MonitorHolders[i].SetShowUiDebugRect(value);
        }

        protected abstract int ClampBufferFrameCount(int raw);

        public void ReconfigureBufferFrameCount(int newBufferFrameCount)
        {
            newBufferFrameCount = ClampBufferFrameCount(newBufferFrameCount);
            if (newBufferFrameCount == BufferFrameCount) return;
            BufferFrameCount = newBufferFrameCount;
            for (var i = 0; i < MonitorHolders.Count; i++)
                MonitorHolders[i].ReconfigureBufferFrameCount(newBufferFrameCount);
        }

        public void Dispose()
        {
            WindowHook.ReleaseHandle();
            WindowHook.AfterResize -= OnAfterResize;
            foreach (var holder in MonitorHolders) holder.Dispose();
            MainForm.MouseDown -= OnMouseDown;
            MainForm.MouseMove -= OnMouseMove;
            MainForm.MouseUp -= OnMouseUp;
        }
    }
}