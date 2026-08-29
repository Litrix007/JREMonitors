using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BveEx.Extensions.ContextMenuHacker;
using BveEx.Extensions.Native;
using BveEx.Extensions.SoundFactory;
using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Builders.Base;
using JREMonitors.BveEx.Configs.Runtime;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Monitors;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Registries;
using JREMonitors.BveEx.Services;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Managers;
using JREMonitors.Core.State;
using ObjectiveHarmonyPatch;
using Vortice.DXGI;
using Vortice.DXGI.Debug;

namespace JREMonitors.BveEx
{
    [Plugin(PluginType.VehiclePlugin)]
    public class PluginMain : AssemblyPluginBase
    {
        private const string TickType = "Tick";
        private const string DrawType = "Draw";
        private readonly AssistantTextWrapper _assistantTextWrapper;
        private readonly BveBlockingService _bveBlockingService;
        private readonly string[] _configPaths;
        private readonly IReadOnlyList<string> _initialConfigReadPaths;
        private readonly RuntimeConfig _runtimeConfig;
        private readonly string _runtimeConfigPath;
        private readonly Stopwatch _sw = new Stopwatch();
        private readonly SynchronizationContext _syncContext;
        private readonly BveTimeProvider _timeProvider;
        private ConfigFileWatcher _configWatcher;
        private IContextMenuHacker _contextMenuHacker;
        private DataHub _dataHub;
        private DebugForm _debugForm;
        private bool _disposed;
        private HarmonyPatch _drawPatch;
        private JumpStationManager _jumpStationManager = new JumpStationManager();
        private string _loadFailedMessage;
        private MonitorContext _monitorContext;
        private BveMonitorManagerBase _monitorManager;
        private INative _native;
        private HarmonyPatch _nativePreviewTickPatch;
        private HarmonyPatch _onDeviceLostPatch;
        private volatile bool _pendingConfigReload;
        private Scenario _scenario;
        private ToolStripMenuItem _showDebugMenuItem;
        private bool _succeed;
        private TickUpdateManager _tickUpdateManager = new TickUpdateManager();
        private IVehicleBuilder _vehicleBuilder;
        private VehicleConfig _vehicleConfig;

        public PluginMain(PluginBuilder builder) : base(builder)
        {
            _dataHub = new DataHub();
            _syncContext = SynchronizationContext.Current;
            var assistantSetClassInfo = BveHacker.BveTypes.GetClassInfoOf<AssistantSet>();
            MethodBase onDeviceLostMethod = assistantSetClassInfo.GetSourceMethodOf("OnDeviceLost").Source;
            _onDeviceLostPatch = HarmonyPatch.Patch(null, onDeviceLostMethod, PatchType.Prefix);
            _onDeviceLostPatch.Invoked += (sender, e) =>
            {
                _monitorManager?.OnDeviceLost();
                return PatchInvokationResult.DoNothing(e);
            };
            _drawPatch = HarmonyPatch.Patch(null,
                BveHacker.BveTypes.GetClassInfoOf<Scenario>().GetSourceMethodOf("Draw").Source, PatchType.Prefix);
            _drawPatch.Invoked += OnDraw;
            _native = Extensions.GetExtension<INative>();
            _native.Started += OnStarted;
            _contextMenuHacker = Extensions.GetExtension<IContextMenuHacker>();
            _bveBlockingService = new BveBlockingService();
            _timeProvider = new BveTimeProvider();
            var runtimeConfigPath = Path.Combine(Path.GetDirectoryName(typeof(PluginMain).Assembly.Location) ?? "",
                "JREMonitorRuntimeConfig.json");
            _runtimeConfigPath = runtimeConfigPath;
            try
            {
                _runtimeConfig = ConfigLoader.LoadConfig<RuntimeConfig>(_runtimeConfigPath, cascadeParent: false)
                                 ?? new RuntimeConfig();
            }
            catch (Exception)
            {
                _runtimeConfig = new RuntimeConfig();
                WriteRuntimeConfig();
            }

            _assistantTextWrapper = new AssistantTextWrapper(BveHacker, _runtimeConfig.InfoAssistant);
            PopulateBaseServices();
            _debugForm = DebugForm.Start(new[] { TickType, DrawType }, BveHacker.MainFormHandle);
            _debugForm.Hidden += OnDebugFormHidden;
            _monitorContext = new MonitorContext(_debugForm, FindBveDxgiAdapter());
            var vehiclePath = BveHacker.ScenarioInfo.VehicleFiles.SelectedFile.Path;
            var mapPath = BveHacker.ScenarioInfo.RouteFiles.SelectedFile.Path;
            var scenarioPath = BveHacker.ScenarioInfo.Path;
            var configPath1 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(vehiclePath) ?? "",
                $"{Path.GetFileNameWithoutExtension(vehiclePath)}.JREMonitorConfig.json"));
            var configPath2 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(mapPath) ?? "",
                $"{Path.GetFileNameWithoutExtension(mapPath)}.JREMonitorConfig.json"));
            var configPath3 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(scenarioPath) ?? "",
                $"{Path.GetFileNameWithoutExtension(scenarioPath)}.JREMonitorConfig.json"));
            _configPaths = new[] { configPath1, configPath2, configPath3 };
            var configRead = false;
            string loadError = null;
            try
            {
                _vehicleConfig = ConfigLoader.LoadConfig<VehicleConfig>(_configPaths, out var readPaths);
                _initialConfigReadPaths = readPaths;
                configRead = true;
                _vehicleConfig.Validate();
                BuildRootAndMonitors(_vehicleConfig);
                ApplyShowDebugWindow(_vehicleConfig.Display.ShowDebugWindow);
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
                DisposeMonitorResources();
                BveHacker.LoadingProgressForm.ThrowError(new LoadError($"Error loading config: {ex.Message}",
                    "JREMonitors", 0, 0));
            }

            BveHacker.PreviewScenarioCreated += OnPreviewScenarioCreated;
            if (configRead)
            {
                var enableHotReload = _vehicleConfig.EnableHotReload;
                if (enableHotReload && _configWatcher == null)
                    _configWatcher = new ConfigFileWatcher(GetWatchPaths(_initialConfigReadPaths),
                        () => _pendingConfigReload = true);
                if (loadError != null)
                    _loadFailedMessage = $"[JREMonitors] {_vehicleConfig.VehicleName} load failed: {loadError}";
            }
            else
            {
                _loadFailedMessage = $"[JREMonitors] Config load failed: {loadError}";
            }

            FixNativeScenarioClosedBug();
        }

        private void FixNativeScenarioClosedBug()
        {
            try
            {
                var nativeExtension = Extensions.GetExtension<INative>();
                if (nativeExtension != null)
                {
                    var nativeType = nativeExtension.GetType();
                    var onPreviewTickMethod = nativeType.GetMethod(
                        "OnPreviewTick",
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(object), typeof(EventArgs) },
                        null
                    );

                    if (onPreviewTickMethod != null)
                    {
                        _nativePreviewTickPatch = HarmonyPatch.Patch(null, onPreviewTickMethod, PatchType.Prefix);
                        _nativePreviewTickPatch.Invoked += (sender, e) =>
                        {
                            if (BveHacker.Scenario == null || !nativeExtension.IsAvailable)
                                return new PatchInvokationResult(SkipModes.SkipOriginal);

                            return PatchInvokationResult.DoNothing(e);
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _debugForm?.AddLineLasting($"Failed to patch Native.OnPreviewTick: {ex.Message}");
            }
        }

        private void WriteRuntimeConfig()
        {
            if (_assistantTextWrapper != null)
            {
                _runtimeConfig.InfoAssistant.Anchor = _assistantTextWrapper.Anchor;
                _runtimeConfig.InfoAssistant.Position = _assistantTextWrapper.Position;
            }

            try
            {
                ConfigLoader.WriteConfig(_runtimeConfigPath, _runtimeConfig);
            }
            catch (Exception ex)
            {
                _debugForm?.AddLineLasting($"Runtime config write failed: {ex}");
            }
        }

        private PatchInvokationResult OnDraw(object sender, PatchInvokedEventArgs e)
        {
            _assistantTextWrapper?.Tick();
            if (_disposed || _monitorManager == null || !_succeed) return PatchInvokationResult.DoNothing(e);
            _debugForm?.SetType(DrawType);
            _debugForm?.ClearLeft(DrawType);
            _debugForm?.ClearLeftDefault();
            _monitorManager.SyncFrame();
            _debugForm?.Commit();
            return PatchInvokationResult.DoNothing(e);
        }

        private static IDXGIAdapter1 FindBveDxgiAdapter()
        {
            var bveVendorId = 0;
            var bveDeviceId = 0;
            var d3d9Device = Direct3DProvider.Instance.Device;
            if (d3d9Device != null)
            {
                var ordinal = d3d9Device.CreationParameters.AdapterOrdinal;
                var identifier = Direct3DProvider.Instance.Direct3D.GetAdapterIdentifier(ordinal);
                bveVendorId = identifier.VendorId;
                bveDeviceId = identifier.DeviceId;
            }

            if (bveVendorId == 0 || bveDeviceId == 0)
                return null;

            using (var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>())
            {
                for (var i = 0;; i++)
                {
                    var result = factory.EnumAdapters1(i, out var adapter);
                    if (result.Failure)
                        break;

                    var desc = adapter.Description1;
                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        adapter.Dispose();
                        continue;
                    }

                    if (desc.VendorId == bveVendorId && desc.DeviceId == bveDeviceId)
                        return adapter;

                    adapter.Dispose();
                }
            }

            return null;
        }


        private void OnPreviewScenarioCreated(ScenarioCreatedEventArgs e)
        {
            _scenario = e.Scenario;
            if (_monitorManager == null)
            {
                ShowLoadFailedMessage();
                return;
            }

            _dataHub.Put(e.Scenario);
            try
            {
                ActivateMonitors(_vehicleConfig);
            }
            catch (Exception ex)
            {
                DisposeMonitorResources();
                BveHacker.LoadingProgressForm.ThrowError(new LoadError($"Error loading config: {ex.Message}",
                    "JREMonitors", 0, 0));
                _loadFailedMessage = $"[JREMonitors] {_vehicleConfig.VehicleName} load failed: {ex.Message}";
                ShowLoadFailedMessage();
                return;
            }

            _succeed = true;
            _assistantTextWrapper.SyncScale();
            _assistantTextWrapper.Attach();
            _assistantTextWrapper.Text = $"[JREMonitors] {_vehicleConfig.VehicleName} Loaded";
        }

        private void ShowLoadFailedMessage()
        {
            if (_loadFailedMessage == null) return;
            _assistantTextWrapper.Text = _loadFailedMessage;
            _assistantTextWrapper.SyncScale();
            _assistantTextWrapper.Attach();
        }

        public override void Tick(TimeSpan elapsed)
        {
            if (_disposed) return;
            if (_pendingConfigReload)
            {
                _pendingConfigReload = false;
                ApplyPendingReload();
            }

            if (_monitorManager == null || !_succeed) return;
            _debugForm?.SetType(TickType);
            _debugForm?.ClearLeft(TickType);
            _debugForm?.ClearLeftDefault();
            _timeProvider.CurrentTime = BveHacker.Scenario.TimeManager.Time;
            _sw.Restart();
            _tickUpdateManager.Update(elapsed);
            _sw.Stop();
            DebugMessages(elapsed);
            _debugForm?.AddLine($"elapsed: {elapsed.TotalMilliseconds}ms");
            _debugForm?.AddLine($"tick update elapsed:{_sw.Elapsed.TotalMilliseconds}ms");
            _monitorManager?.DrawMonitors(elapsed);
        }

        [Conditional("DEBUG")]
        private void DebugMessages(TimeSpan elapsed)
        {
            _debugForm?.AddLine($"InertiaRatio: {_scenario.Vehicle.Dynamics.InertiaRatio.Value:F4}");
            _debugForm?.AddLine($"TotalMass: {_scenario.Vehicle.Dynamics.TotalMass:F0} kg");
            _debugForm?.AddLine($"PassengerLoad: {_scenario.Vehicle.Passenger.Load.Value:F0} kg");
            _debugForm?.AddLine(
                $"power: {_scenario.Vehicle.Instruments.AtsPlugin.AtsHandles.PowerNotch}; brake： {_scenario.Vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch}");
            _debugForm?.AddLine(
                $"{_scenario.Vehicle.Dynamics.MotorCar.Count}M{_scenario.Vehicle.Dynamics.TrailerCar.Count}T");
            _debugForm?.AddLine($"passenger count: {BveHacker.Scenario.Vehicle.Conductor.Passenger.Count}");
            _debugForm?.AddLine(
                $"passenger total count: {BveHacker.Scenario.Vehicle.Conductor.Passenger.Count * (BveHacker.Scenario.Vehicle.Dynamics.MotorCar.Count + BveHacker.Scenario.Vehicle.Dynamics.TrailerCar.Count)}");
            _debugForm?.AddLine($"passenger load: {BveHacker.Scenario.Vehicle.Conductor.Passenger.Load.Value}");
            var leftDoors = BveHacker.Scenario.Vehicle.Conductor.Doors.GetSide(DoorSide.Left);
            var l2 = leftDoors.CarDoors.ToListSafe();
            var rightDoors = BveHacker.Scenario.Vehicle.Conductor.Doors.GetSide(DoorSide.Right);
            var r2 = rightDoors.CarDoors.ToListSafe();
            _debugForm?.AddLine($"left door is open:{leftDoors.IsOpen}");
            _debugForm?.AddLine($"left door is open2:{string.Join(",", l2.Select(d => d.IsOpen ? "Open" : "Close"))}");
            _debugForm?.AddLine($"left door close time:{string.Join(",", l2.Select(d => d.CloseTime))}");
            _debugForm?.AddLine(
                $"left door close time left:{string.Join(",", l2.Select(d => d.TimeLeftToCompleteClosing))}");
            _debugForm?.AddLine($"right door is open:{rightDoors.IsOpen}");
            _debugForm?.AddLine($"right door is open2:{string.Join(",", r2.Select(d => d.IsOpen ? "Open" : "Close"))}");
            _debugForm?.AddLine($"right door close time:{string.Join(",", r2.Select(d => d.CloseTime))}");
            _debugForm?.AddLine(
                $"right door close time left:{string.Join(",", r2.Select(d => d.TimeLeftToCompleteClosing))}");
        }

        private void OnStarted(object sender, StartedEventArgs e)
        {
            InvokeJumpStationActions();
        }

        private void InvokeJumpStationActions()
        {
            _jumpStationManager.InvokeListeners();
        }

        private void ApplyPendingReload()
        {
            VehicleConfig newConfig;
            IReadOnlyList<string> readPaths;
            try
            {
                newConfig = ConfigLoader.LoadConfig<VehicleConfig>(_configPaths, out readPaths);
                newConfig.Validate();
            }
            catch (Exception ex)
            {
                _debugForm?.AddLineLasting($"Hot reload failed: {ex}");
                NotifyHotReloadFailed(ex.Message);
                return;
            }

            var error = _monitorManager == null || newConfig.GetType() != _vehicleConfig?.GetType()
                ? FullRebuild(newConfig)
                : ReconfigureSameVehicle(newConfig);
            if (error == null)
            {
                _configWatcher?.UpdatePaths(GetWatchPaths(readPaths));
                _loadFailedMessage = null;
                NotifyHotReloadSucceeded();
            }
            else
            {
                NotifyHotReloadFailed(error);
            }
        }

        private IEnumerable<string> GetWatchPaths(IReadOnlyList<string> readPaths)
        {
            return _configPaths
                .Concat(readPaths ?? Array.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void NotifyHotReloadSucceeded()
        {
            SetInfoAssistantText($"[JREMonitors] {_vehicleConfig.VehicleName} Hot reloaded", Color.LimeGreen);
        }

        private void NotifyHotReloadFailed(string error = null)
        {
            var text = string.IsNullOrEmpty(error)
                ? "[JREMonitors] Hot reload failed"
                : $"[JREMonitors] Hot reload failed: {error}";
            SetInfoAssistantText(text, Color.Red);
        }

        private void SetInfoAssistantText(string text, Color color)
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text = text;
            _assistantTextWrapper.Alert(color);
        }

        private string ReconfigureSameVehicle(VehicleConfig newConfig)
        {
            var context = new VehicleBuildContext(_dataHub, _tickUpdateManager, _jumpStationManager);
            try
            {
                _vehicleBuilder.Reconfigure(context, _vehicleConfig, newConfig);
                _monitorManager.Reconfigure(_vehicleBuilder, context, newConfig);
                ReconfigureDisplay(_vehicleConfig.Display, newConfig.Display);
                _bveBlockingService.Clear();
                _vehicleConfig = newConfig;
                ReorderContextMenuItems();
                _debugForm?.AddLineLasting("Hot reload(same vehicle) succeed.");
                return null;
            }
            catch (Exception ex)
            {
                _debugForm?.AddLineLasting($"Hot reload failed: {ex}");
                return ex.Message;
            }
        }

        private string FullRebuild(VehicleConfig newConfig)
        {
            _succeed = false;
            try
            {
                DisposeMonitorResources();
                PopulateBaseServices();
                _dataHub.Put(_scenario);
                DisposeShowDebugMenuItem();
                BuildRootAndMonitors(newConfig);
                ActivateMonitors(newConfig);
                ApplyShowDebugWindow(newConfig.Display.ShowDebugWindow);
                _vehicleConfig = newConfig;
                ReorderContextMenuItems();
                _debugForm?.AddLineLasting("Hot reload(full rebuild) succeed.");
                _succeed = true;
                return null;
            }
            catch (Exception ex)
            {
                DisposeMonitorResources();
                _debugForm?.AddLineLasting($"Hot reload failed: {ex}");
                _vehicleConfig = null;
                return ex.Message;
            }
        }

        private void DisposeMonitorResources()
        {
            _monitorManager?.DetachMonitors();
            _monitorManager?.Dispose();
            _monitorManager = null;
            _tickUpdateManager.Clear();
            _jumpStationManager.Clear();
            _dataHub.Clear();
            _monitorContext.DisposeProperties();
        }

        private void DisposeShowDebugMenuItem()
        {
            if (_showDebugMenuItem == null) return;
            _showDebugMenuItem.CheckedChanged -= OnShowDebugMenuItemCheckedChanged;
            _showDebugMenuItem.Dispose();
            _showDebugMenuItem = null;
        }

        private void BuildRootAndMonitors(VehicleConfig config)
        {
            _dataHub.Put(_debugForm);
            _vehicleBuilder = VehicleBuilderRegistry.GetBuilder(config);
            var context = new VehicleBuildContext(_dataHub, _tickUpdateManager, _jumpStationManager);
            _vehicleBuilder.PopulateRootDataHub(context, config);
            var monitorProperties = _vehicleBuilder.CreateMonitorProperties(_monitorContext, config, _dataHub)
                .OrderBy(p => config.MonitorIdOrders.TryGetValue(p.Id, out var o) ? o : int.MaxValue)
                .ToList();
            _monitorManager = BveMonitorManagerFactory.Create(
                _dataHub, _monitorContext, monitorProperties, _timeProvider,
                config.Display.ShowCabTextureBoundsRect, config.Display.BufferFrameCount);
            _vehicleBuilder.PopulateMonitorLocalDataHub(context, _monitorManager.Monitors, config);
        }

        private void ActivateMonitors(VehicleConfig config)
        {
            _vehicleBuilder.PostPopulateRootDataHub(
                new VehiclePostBuildContext(_dataHub, _tickUpdateManager, _jumpStationManager), config);
            InvokeJumpStationActions();
            _monitorManager.Initialize(_scenario);
            _monitorManager.AttachMonitors();
            _debugForm?.SetType(TickType);
            _debugForm?.ClearLeft(TickType);
            _monitorManager.DrawMonitors(TimeSpan.Zero);
            _debugForm?.SetType(DrawType);
            _debugForm?.ClearLeft(DrawType);
            _monitorManager.SyncFrame();
        }

        private void ReconfigureDisplay(DisplayConfig oldDisplay, DisplayConfig newDisplay)
        {
            if (oldDisplay.ShowCabTextureBoundsRect != newDisplay.ShowCabTextureBoundsRect)
                _monitorManager.SetShowTextureBoundsRect(newDisplay.ShowCabTextureBoundsRect);
            if (oldDisplay.ShowUiDebugRect != newDisplay.ShowUiDebugRect)
                _monitorManager.SetShowUiDebugRect(newDisplay.ShowUiDebugRect);
            if (oldDisplay.BufferFrameCount != newDisplay.BufferFrameCount)
                _monitorManager.ReconfigureBufferFrameCount(newDisplay.BufferFrameCount);
            if (oldDisplay.ShowDebugWindow != newDisplay.ShowDebugWindow)
                ApplyShowDebugWindow(newDisplay.ShowDebugWindow);
        }

        private void PopulateBaseServices()
        {
            _tickUpdateManager.Register(_bveBlockingService);
            _jumpStationManager.Register(_bveBlockingService);
            _dataHub.Put(_bveBlockingService);
            _dataHub.Put(_native);
            _dataHub.Put(new Lazy<AtsPlugin>(() => _scenario.Vehicle.Instruments.AtsPlugin));
            _dataHub.Put(_timeProvider);
            _dataHub.Put(BveHacker);
            _dataHub.Put(_assistantTextWrapper);
            _dataHub.Put(Extensions.GetExtension<ISoundFactory>());
            _dataHub.Put(_contextMenuHacker);
        }

        private void ApplyShowDebugWindow(bool value)
        {
            if (value)
            {
                if (_showDebugMenuItem == null)
                {
                    _showDebugMenuItem = _contextMenuHacker.AddCheckableMenuItem(
                        "Show debug window", OnShowDebugMenuItemCheckedChanged, ContextMenuItemType.Plugins);
                    _showDebugMenuItem.Tag = MonitorHolderBase.MenuTagPrefix + "debug";
                }

                if (!_showDebugMenuItem.Checked)
                    _showDebugMenuItem.Checked = true;
            }
            else if (_showDebugMenuItem != null && _showDebugMenuItem.Checked)
            {
                _showDebugMenuItem.Checked = false;
            }
        }

        private void ReorderContextMenuItems()
        {
            var mainForm = BveHacker.MainForm;
            if (mainForm?.ContextMenu == null || _monitorManager == null) return;
            var items = mainForm.ContextMenu.Items;
            var selfIndices = new List<int>();
            for (var i = 0; i < items.Count; i++)
                if (items[i].Tag is string tag &&
                    tag.StartsWith(MonitorHolderBase.MenuTagPrefix, StringComparison.Ordinal))
                    selfIndices.Add(i);
            if (selfIndices.Count == 0) return;
            var insertIndex = selfIndices.Min();
            var entries = _monitorManager.Holders
                .Select(h => (item: h.ContextMenuCheckbox, id: h.Monitor.Id))
                .Where(e => e.item != null && !e.item.IsDisposed)
                .ToList();
            entries.Sort((a, b) => GetMonitorOrder(a.id).CompareTo(GetMonitorOrder(b.id)));
            var orderedItems = entries.Select(e => (ToolStripItem)e.item).ToList();
            if (_showDebugMenuItem != null && !_showDebugMenuItem.IsDisposed)
                orderedItems.Add(_showDebugMenuItem);
            foreach (var idx in selfIndices.OrderByDescending(i => i)) items.RemoveAt(idx);
            for (var i = 0; i < orderedItems.Count; i++) items.Insert(insertIndex + i, orderedItems[i]);
        }

        private int GetMonitorOrder(string monitorId)
        {
            return _vehicleConfig.MonitorIdOrders.TryGetValue(monitorId, out var order) ? order : int.MaxValue;
        }

        private void OnShowDebugMenuItemCheckedChanged(object sender, EventArgs e)
        {
            if (_debugForm == null) return;
            if (_showDebugMenuItem.Checked) _debugForm.Show();
            else _debugForm.Hide();
        }

        private void OnDebugFormHidden()
        {
            _syncContext.Post(_ =>
            {
                if (_showDebugMenuItem != null && !_showDebugMenuItem.IsDisposed && _showDebugMenuItem.Checked)
                    _showDebugMenuItem.Checked = false;
            }, null);
        }

        private void DisposeDebugForm()
        {
            DisposeShowDebugMenuItem();
            if (_debugForm != null && !_debugForm.IsDisposed) _debugForm.ForceClose();
            _debugForm = null;
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            WriteRuntimeConfig();
            _assistantTextWrapper?.Dispose();
            _configWatcher?.Dispose();
            _configWatcher = null;
            _monitorManager?.DetachMonitors();
            _monitorManager?.Dispose();
            _monitorManager = null;
            _dataHub?.Dispose();
            _dataHub = null;
            _scenario = null;
            _vehicleConfig = null;
            _vehicleBuilder = null;
            BveHacker.PreviewScenarioCreated -= OnPreviewScenarioCreated;
            DisposeDebugForm();
            _jumpStationManager?.Clear();
            _jumpStationManager = null;
            _tickUpdateManager?.Clear();
            _tickUpdateManager = null;
            if (_native != null)
            {
                _native.Started -= OnStarted;
                _native = null;
            }

            _nativePreviewTickPatch?.Dispose();
            _nativePreviewTickPatch = null;
            _contextMenuHacker = null;
            _onDeviceLostPatch?.Dispose();
            _onDeviceLostPatch = null;
            _drawPatch?.Dispose();
            _drawPatch = null;
            _monitorContext?.Dispose();
            _monitorContext = null;
#if DEBUG
            using (var dxgiDebug = DXGI.DXGIGetDebugInterface1<IDXGIDebug1>())
            {
                Debug.WriteLine("===== [DXGI REPORT LIVE OBJECTS START] =====");
                dxgiDebug.ReportLiveObjects(DXGI.DebugAll,
                    ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.Detail |
                    ReportLiveObjectFlags.IgnoreInternal);
                Debug.WriteLine("===== [DXGI REPORT LIVE OBJECTS END] =====");
            }
#endif
        }
    }
}