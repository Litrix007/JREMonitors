using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct3D11;
using Monitor = JREMonitors.Core.Monitors.Monitor;

namespace JREMonitors.SandBox
{
    public class SandboxMonitorManager : MonitorManager
    {
        private readonly MonitorContext _context;
        private readonly IDebugger _debugger;
        private readonly PendingClickQueue<MonitorInfo> _extClickQueue = new PendingClickQueue<MonitorInfo>();
        private readonly List<MonitorInfo> _monitorInfos = new List<MonitorInfo>();

        public SandboxMonitorManager(MonitorContext context, DataHub dataHub, IntPtr handle,
            IEnumerable<(Monitor monitor, Size size)> monitors) : base(context, dataHub)
        {
            _context = context;
            _debugger = dataHub.GetOrNull<IDebugger>();
            foreach (var (monitor, size) in monitors)
            {
                var output = new MonitorOutput(size, context.D2D1Context, context.D3D11Device);
                monitor.AddOutput(output);
                var extSharedTexture =
                    context.D3D11Device.CreateTexture2D(
                        RenderHelper.CreateRenderTargetTextureDescription(size.Width, size.Height));
                var form = new ExternalDisplayForm(context, new[]
                    {
                        new Size(800, 600),
                        new Size(1024, 768),
                        new Size(1440, 1080),
                        new Size(1600, 1200),
                        new Size(1920, 1440)
                    }, size, false, handle, monitor.Id,
                    ScreenDisplayMode.Letterbox);
                form.SetSharedTexture(extSharedTexture);
                var info = new MonitorInfo
                {
                    Monitor = monitor,
                    Output = output,
                    ExtSharedTexture = extSharedTexture,
                    Form = form
                };
                form.OnLeftClick += (s, e) => OnFormLeftClick(info, e);
                form.Start();
                _monitorInfos.Add(info);
            }
        }

        private void OnFormLeftClick(MonitorInfo info, MouseEventArgs e)
        {
            var mapped = ExternalDisplayForm.MapClickPosition(
                ScreenDisplayMode.Letterbox,
                e.X, e.Y,
                info.Form.ClientSize.Width, info.Form.ClientSize.Height,
                info.Output.Size.Width, info.Output.Size.Height);

            if (mapped.HasValue)
                _extClickQueue.TryEnqueue(info, mapped.Value);
        }

        public virtual void Render(TimeSpan elapsed)
        {
            ProcessPendingClick();
            _context.D2D1Context.BeginDraw();
            foreach (var info in _monitorInfos)
            {
                var sw = Stopwatch.StartNew();
                info.Monitor.Draw(elapsed, false);
                sw.Stop();
                _debugger?.AddLine(
                    $"{info.Monitor.Id} frozen:{info.Output.Frozen}; draw: {sw.Elapsed.TotalMilliseconds}");
            }

            _context.D2D1Context.EndDraw();
            _context.D2D1Context.Target = null;
            foreach (var info in _monitorInfos)
            {
                if (!info.HasShown && info.Form.IsHwndReady)
                    info.HasShown = true;

                if (info.HasShown && !info.Form.IsDisposed)
                {
                    _context.D3D11Context.CopyResource(info.ExtSharedTexture, info.Output.OutputD3D11Texture);
                    info.Form.Brightness = info.Monitor.Brightness;
                    info.Form.NotifyFrameReady();
                }
            }
        }

        private void ProcessPendingClick()
        {
            if (_extClickQueue.TryDequeue(out var info, out var pos))
                info.Monitor.Click(info.Output.Size, pos);
        }

        public void SaveSnapshots()
        {
            foreach (var info in _monitorInfos)
            {
                var output = info.Output;
                var stamp = Stopwatch.StartNew();
                var path = SaveSnapshot(info.Monitor.Id, output);
                stamp.Stop();
                _debugger?.AddLineLasting(
                    $"{info.Monitor.Id} snapshot saved: {path} ({stamp.Elapsed.TotalMilliseconds:0.0}ms)");
            }
        }

        private string SaveSnapshot(string monitorId, MonitorOutput output)
        {
            var width = output.Size.Width;
            var height = output.Size.Height;
            var staging = _context.D3D11Device.CreateTexture2D(
                RenderHelper.CreateStagingTextureDescription(width, height));
            try
            {
                _context.D3D11Context.CopyResource(staging, output.OutputD3D11Texture);
                var mapped = _context.D3D11Context.Map(staging, 0);
                try
                {
                    var path = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, $"snapshot-{monitorId}.png");
                    using (var bitmap = new Bitmap(width, height, mapped.RowPitch, PixelFormat.Format32bppArgb,
                               mapped.DataPointer))
                    {
                        bitmap.Save(path, ImageFormat.Png);
                    }

                    return path;
                }
                finally
                {
                    _context.D3D11Context.Unmap(staging, 0);
                }
            }
            finally
            {
                staging.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (var info in _monitorInfos)
            {
                info.Form?.Dispose();
                info.ExtSharedTexture?.Dispose();
                info.Monitor.Dispose();
            }

            _monitorInfos.Clear();
        }

        private class MonitorInfo
        {
            public ID3D11Texture2D ExtSharedTexture;
            public ExternalDisplayForm Form;
            public bool HasShown;
            public Monitor Monitor;
            public MonitorOutput Output;
        }
    }
}