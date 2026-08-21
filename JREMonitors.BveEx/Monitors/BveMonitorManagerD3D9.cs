using System;
using System.Collections.Generic;
using System.Drawing;
using BveTypes.ClassWrappers;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils.Render;
using SlimDX.Direct3D9;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using D3D9Format = SlimDX.Direct3D9.Format;
using D3D9Usage = SlimDX.Direct3D9.Usage;
using D3D9Pool = SlimDX.Direct3D9.Pool;
using Vector2 = System.Numerics.Vector2;

namespace JREMonitors.BveEx.Monitors
{
    public class BveMonitorManagerD3D9 : BveMonitorManagerBase
    {
        public BveMonitorManagerD3D9(DataHub dataHub, MonitorContext context, IEnumerable<MonitorProperties> props,
            ITimeProvider time, bool debugRect, int rawBufferFrameCount) : base(dataHub, context, props, time,
            debugRect,
            rawBufferFrameCount)
        {
        }

        protected override MonitorHolderBase CreateHolder(
            MonitorProperties config, ITimeProvider timeProvider, bool showTextureBoundsRect,
            int bufferFrameCount, Action<MonitorHolderBase, Vector2> externalClickCallback)
        {
            return new MonitorHolderD3D9(DataHub, Context, config, timeProvider, showTextureBoundsRect,
                bufferFrameCount, externalClickCallback);
        }

        protected override int ClampBufferFrameCount(int raw)
        {
            return raw == 0 ? 2 : MathHelper.Clamp(raw, 1, 3);
        }
    }

    public class MonitorHolderD3D9 : MonitorHolderBase
    {
        private bool _hadConsumers;
        private D3D9RingBuffer _ringBuffer;

        public MonitorHolderD3D9(DataHub dataHub, MonitorContext context, MonitorProperties properties,
            ITimeProvider timeProvider, bool showDebugRect, int bufferFrameCount,
            Action<MonitorHolderBase, Vector2> externalClickCallback)
            : base(dataHub, context, properties, timeProvider, showDebugRect, externalClickCallback, bufferFrameCount)
        {
        }

        protected override void CreateCabD3D11Texture()
        {
            if (TexWidth <= 0 || TexHeight <= 0) return;

            var bufferSize = Properties.Cab.Enabled ? new Size(TexWidth, TexHeight) : Size.Empty;
            _ringBuffer?.Dispose();
            _ringBuffer = new D3D9RingBuffer(Context, Properties.Cab.Enabled, bufferSize,
                BufferFrameCount);
            _hadConsumers = true;
            if (Properties.Cab.Enabled)
            {
                base.DisposeCabTextures();

                CabD3D11Texture = Context.D3D11Device.CreateTexture2D(
                    RenderHelper.CreateRenderTargetTextureDescription(TexWidth, TexHeight));

                using (var dxgiSurface = CabD3D11Texture.QueryInterface<IDXGISurface>())
                {
                    CabD2D1TransformBitmap =
                        Context.D2D1Context.CreateBitmapFromDxgiSurface(dxgiSurface, RenderHelper.BitMapProperties8Bit);
                }
            }
        }

        public override void EnsureD3D9Texture()
        {
            if (!Properties.Cab.Enabled || TexWidth <= 0 || TexHeight <= 0) return;
            if (D3D9MergedTexture != null || DaytimeModel == null) return;
            D3D9MergedTexture = new Texture(
                Direct3DProvider.Instance.Device,
                TexWidth,
                TexHeight,
                1,
                D3D9Usage.Dynamic,
                D3D9Format.A8R8G8B8,
                D3D9Pool.Default
            );
            D3D9TextureRecreated = true;
            DaytimeModel.Materials[0].Texture = D3D9MergedTexture;
        }

        public override void Submit()
        {
            if (!Actions.ShouldSubmit) return;
            if (_ringBuffer == null) return;
            if (!Properties.Cab.Enabled && (ExternalForm == null || !ExternalForm.IsVisible)) return;
            Submitted = true;
            _ringBuffer.Submit(CabD3D11Texture);
            Debugger?.AddLine($"{Monitor.Id} submit");
        }

        public override void Sync()
        {
            var isCabEnabled = Properties.Cab.Enabled;
            var hasExtForm = ExternalForm != null && !ExternalForm.IsDisposed;
            var hasConsumers = isCabEnabled || (hasExtForm && ExternalForm.IsVisible);

            if (!hasConsumers)
            {
                _hadConsumers = false;
                return;
            }

            var isTransition = !_hadConsumers;
            if (isTransition)
            {
                _ringBuffer?.Reset();
                _hadConsumers = true;
            }

            var hasMappedResource = false;
            MappedSubresource mappedResource = default;
            D3D9RingBuffer.BufferSlot readSlot = null;
            if (_ringBuffer != null && !isTransition)
                if (D3D9TextureRecreated || Actions.ShouldSync || _ringBuffer.UnmappedCount > 0)
                {
                    D3D9TextureRecreated = false;
                    mappedResource = _ringBuffer.Map(out readSlot);
                    hasMappedResource = true;
                }

            if (isCabEnabled && D3D9MergedTexture != null && hasMappedResource && readSlot != null)
            {
                Debugger?.AddLine($"{Monitor.Id} sync texture");
                var rect = D3D9MergedTexture.LockRectangle(0, LockFlags.Discard);
                try
                {
                    RenderHelper.CopyTexture(
                        mappedResource.DataPointer,
                        rect.Data.DataPointer,
                        mappedResource.RowPitch,
                        rect.Pitch,
                        TexWidth,
                        TexHeight
                    );
                }
                finally
                {
                    D3D9MergedTexture.UnlockRectangle(0);
                }
            }

            if (hasExtForm)
                if (Actions.ShouldSyncExternal || isTransition || ExtFormNeedsSync)
                {
                    Debugger?.AddLine($"{Monitor.Id} sync external");
                    SyncExternalCopy(MonitorGameOutput.OutputD3D11Texture);
                }


            if (hasMappedResource && _ringBuffer != null) _ringBuffer.Unmap();
        }

        public override void Detach()
        {
            base.Detach();
            _ringBuffer?.Reset();
        }

        /// <summary>
        ///     热重载 BufferFrameCount：重建 D3D9RingBuffer 与 D3D9 纹理，重置 Sync 状态机。
        ///     CreateCabD3D11Texture 会 Dispose 旧 _ringBuffer 并按新 BufferFrameCount 重建。
        /// </summary>
        public override void ReconfigureBufferFrameCount(int newBufferFrameCount)
        {
            BufferFrameCount = newBufferFrameCount;
            DisposeD3D9Texture();
            D3D9MergedTexture?.Dispose();
            D3D9MergedTexture = null;
            D3D9TextureRecreated = true;
            CreateCabD3D11Texture();
            EnsureD3D9Texture();
            _ringBuffer?.Reset();
            IsCabProjectionDirty = true;
        }

        public override void Dispose()
        {
            _ringBuffer?.Dispose();
            _ringBuffer = null;
            base.Dispose();
        }
    }
}