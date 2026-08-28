using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Lighting;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Shadows;
using SharpGen.Runtime;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.WIC;
using FeatureLevel = Vortice.Direct3D.FeatureLevel;

namespace JREMonitors.Core.Contexts
{
    public class MonitorContext : IDisposable
    {
        private IDXGIAdapter _adapter;

        public MonitorContext(IDebugger debugger = null, IDXGIAdapter adapter = null)
        {
            Debugger = debugger;
            _adapter = adapter;
            var driverType = adapter != null ? DriverType.Unknown : DriverType.Hardware;

            // VideoSupport 供后续视频编码管线（ID3D11VideoDevice / MF 硬编 MFT）使用；
            // 病态无视频能力的设备（虚拟显卡等）上带此 flag 创建会失败，回退到无 flag 重建（视频功能届时不可用）。
            // 纯功能性声明：不改变渲染路径性能，不启用任何视频硬件任务。
            var creationFlags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;
#if DEBUG
            creationFlags |= DeviceCreationFlags.Debug;
#endif
            ID3D11Device d3D11Device;
            ID3D11DeviceContext d3DContext;
            try
            {
                D3D11.D3D11CreateDevice(
                    adapter,
                    driverType,
                    creationFlags,
                    new[] { FeatureLevel.Level_11_1 },
                    out d3D11Device,
                    out d3DContext
                );
                SupportsVideo = true;
            }
            catch (SharpGenException)
            {
                creationFlags &= ~DeviceCreationFlags.VideoSupport;
                D3D11.D3D11CreateDevice(
                    adapter,
                    driverType,
                    creationFlags,
                    new[] { FeatureLevel.Level_11_1 },
                    out d3D11Device,
                    out d3DContext
                );
                SupportsVideo = false;
                Debugger?.AddLineLasting(
                    "D3D11 device created without VideoSupport; video encode pipeline unavailable");
            }

            using (var multithread = d3D11Device.QueryInterface<ID3D11Multithread>())
            {
                multithread.SetMultithreadProtected(true);
            }

            D3D11Device = d3D11Device;
            D3D11Context = d3DContext;
            DxgiDevice = d3D11Device.QueryInterface<IDXGIDevice>();
            D2D1Factory = D2D1.D2D1CreateFactory<ID2D1Factory1>(FactoryType.MultiThreaded);
            D2D1Device = D2D1Factory.CreateDevice(DxgiDevice);
            D2D1Context = D2D1Device.CreateDeviceContext();
            D2D1Context.Dpi = new SizeF(96, 96);
            Brush = D2D1Context.CreateSolidColorBrush(default);
            WicImagingFactory = new IWICImagingFactory();
            DropShadowProcessor = new DropShadowProcessor(D2D1Context);
            InnerShadowProcessor.Register(D2D1Factory);
            AdaptiveScreenLightingEffect.Register(D2D1Factory);
            GhostingEffect.Register(D2D1Factory);
            InnerShadowProcessor = new InnerShadowProcessor(D2D1Context);
            var assemblyDir = Path.GetDirectoryName(typeof(MonitorContext).Assembly.Location);
            debugger?.AddLineLasting($"Font directory path: {assemblyDir}");
            FontManager = new FontManager(Path.Combine(assemblyDir, "Fonts"), debugger);
            var queryDesc = new QueryDescription
            {
                QueryType = QueryType.Event,
                MiscFlags = QueryFlags.None
            };
            D3D11FenceQuery = D3D11Device.CreateQuery(queryDesc);
        }

        public IDebugger Debugger { get; }
        public Dictionary<PropertyKey, object> Properties { get; private set; } = new Dictionary<PropertyKey, object>();

        /// <summary>
        ///     D3D11 设备是否带 <c>VideoSupport</c> flag 创建成功（可供视频编码管线使用）。
        ///     无视频能力的病态设备上回退为 false，视频相关功能届时应检查此属性并禁用。
        /// </summary>
        public bool SupportsVideo { get; private set; }

        public ID3D11Device D3D11Device { get; private set; }
        public ID3D11Query D3D11FenceQuery { get; private set; }
        public ID3D11DeviceContext D3D11Context { get; private set; }
        public IDXGIDevice DxgiDevice { get; private set; }
        public ID2D1Factory1 D2D1Factory { get; private set; }
        public ID2D1Device D2D1Device { get; private set; }
        public ID2D1DeviceContext D2D1Context { get; private set; }
        public ID2D1SolidColorBrush Brush { get; private set; }
        public IWICImagingFactory WicImagingFactory { get; private set; }
        public DropShadowProcessor DropShadowProcessor { get; private set; }
        public InnerShadowProcessor InnerShadowProcessor { get; private set; }
        public FontManager FontManager { get; private set; }

        public void Dispose()
        {
            DisposeProperties();
            Properties = null;
            D3D11FenceQuery?.Dispose();
            D3D11FenceQuery = null;
            FontManager?.Dispose();
            FontManager = null;
            InnerShadowProcessor?.Dispose();
            InnerShadowProcessor = null;
            DropShadowProcessor?.Dispose();
            DropShadowProcessor = null;
            WicImagingFactory?.Dispose();
            WicImagingFactory = null;
            Brush?.Dispose();
            Brush = null;
            if (D2D1Context != null)
            {
                D2D1Context.Target = null;
                D2D1Context.Dispose();
                D2D1Context = null;
            }

            if (D2D1Device != null)
            {
                D2D1Device.Dispose();
                D2D1Device = null;
            }

            if (D2D1Factory != null)
            {
                InnerShadowProcessor.Unregister(D2D1Factory);
                AdaptiveScreenLightingEffect.Unregister(D2D1Factory);
                GhostingEffect.Unregister(D2D1Factory);
                D2D1Factory.Dispose();
                D2D1Factory = null;
            }

            DxgiDevice?.Dispose();
            DxgiDevice = null;
            if (D3D11Context != null)
            {
                D3D11Context.ClearState();
                D3D11Context.Flush();
                D3D11Context.Dispose();
                D3D11Context = null;
            }

            if (D3D11Device != null)
            {
                D3D11Device.Dispose();
                D3D11Device = null;
            }

            _adapter?.Dispose();
            _adapter = null;
        }

        public void DisposeProperties()
        {
            if (Properties != null)
            {
                foreach (var value in Properties.Values)
                    if (value is IDisposable disposable)
                        disposable.Dispose();

                Properties.Clear();
            }
        }
    }
}