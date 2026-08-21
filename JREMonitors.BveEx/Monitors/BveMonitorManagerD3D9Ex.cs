using System;
using System.Collections.Generic;
using System.Drawing;
using BveTypes.ClassWrappers;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using SlimDX.Direct3D9;
using Vortice.Mathematics;
using D3D9Format = SlimDX.Direct3D9.Format;
using D3D9Usage = SlimDX.Direct3D9.Usage;
using D3D9Pool = SlimDX.Direct3D9.Pool;
using Vector2 = System.Numerics.Vector2;

namespace JREMonitors.BveEx.Monitors
{
    public class BveMonitorManagerD3D9Ex : BveMonitorManagerBase
    {
        public BveMonitorManagerD3D9Ex(DataHub dataHub, MonitorContext context, IEnumerable<MonitorProperties> props,
            ITimeProvider time, bool debugRect, int rawBufferFrameCount) : base(dataHub, context, props, time,
            debugRect,
            rawBufferFrameCount)
        {
        }

        protected override MonitorHolderBase CreateHolder(
            MonitorProperties config, ITimeProvider timeProvider, bool showTextureBoundsRect,
            int bufferFrameCount, Action<MonitorHolderBase, Vector2> externalClickCallback)
        {
            return new MonitorHolderD3D9Ex(DataHub, Context, config, timeProvider, showTextureBoundsRect,
                bufferFrameCount, externalClickCallback);
        }

        protected override int ClampBufferFrameCount(int raw)
        {
            return raw == 0 ? 3 : MathHelper.Clamp(raw, 3, 5);
        }
    }

    public class MonitorHolderD3D9Ex : MonitorHolderBase
    {
        private bool _hadConsumers;
        private bool _hasPendingAcquisition;
        private D3D9ExSharedBuffer _sharedBuffer;

        public MonitorHolderD3D9Ex(DataHub dataHub, MonitorContext context, MonitorProperties properties,
            ITimeProvider timeProvider, bool showDebugRect, int bufferFrameCount,
            Action<MonitorHolderBase, Vector2> externalClickCallback)
            : base(dataHub, context, properties, timeProvider, showDebugRect, externalClickCallback, bufferFrameCount)
        {
        }

        protected override void CreateCabD3D11Texture()
        {
            if (TexWidth <= 0 || TexHeight <= 0) return;
            var cabSize = Properties.Cab.Enabled ? new Size(TexWidth, TexHeight) : Size.Empty;
            _sharedBuffer?.Dispose();
            _sharedBuffer = new D3D9ExSharedBuffer(Context, Properties.Cab.Enabled, cabSize,
                BufferFrameCount);
            _hadConsumers = true;
            UpdateRenderTargets();
        }

        /// <summary>
        ///     D3D11 写槽背压：下一个写槽上一次提交的 fence 未完成（GPU 落后 ≥ BufferFrameCount 帧提交）时停产。
        ///     该 D3D11 设备只渲染到纹理、无 Present 节流点，队列可无界增长——此检查是唯一的积压防线。
        /// </summary>
        protected override bool IsProductionBlocked()
        {
            return _sharedBuffer != null && _sharedBuffer.IsNextWriteSlotPending();
        }

        private void UpdateRenderTargets()
        {
            if (_sharedBuffer == null) return;
            var writeSlot = _sharedBuffer.AcquireWriteSlot();
            if (Properties.Cab.Enabled)
            {
                CabD3D11Texture = writeSlot.ProjectedD3D11Texture;
                CabD2D1TransformBitmap = writeSlot.ProjectedD2D1Bitmap;
            }
        }

        protected override void DisposeCabTextures()
        {
            CabD2D1TransformBitmap = null;
            CabD3D11Texture = null;
        }

        public override void EnsureD3D9Texture()
        {
            if (!Properties.Cab.Enabled || TexWidth <= 0 || TexHeight <= 0 || DaytimeModel == null) return;
            if (D3D9MergedTexture == null)
            {
                D3D9MergedTexture = new Texture(
                    Direct3DProvider.Instance.Device,
                    TexWidth, TexHeight, 1,
                    D3D9Usage.RenderTarget, D3D9Format.A8R8G8B8, D3D9Pool.Default
                );
                DaytimeModel.Materials[0].Texture = D3D9MergedTexture;
                D3D9TextureRecreated = true;
            }

            _sharedBuffer?.EnsureD3D9Textures(Direct3DProvider.Instance.Device);
        }

        public override void Submit()
        {
            if (!Actions.ShouldSubmit) return;
            if (!Properties.Cab.Enabled && (ExternalForm == null || !ExternalForm.IsVisible)) return;
            Submitted = true;
            _sharedBuffer?.SubmitWriteSlot();
            UpdateRenderTargets();
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
                _sharedBuffer?.PrepareTransition();
                _hadConsumers = true;
            }

            if (!Actions.ShouldSync && !isTransition && !D3D9TextureRecreated && !_hasPendingAcquisition &&
                !ExtFormNeedsSync) return;
            D3D9TextureRecreated = false;
            var readSlot = _sharedBuffer?.TryAcquireDisplaySlot();
            _hasPendingAcquisition = _sharedBuffer?.HasNewerPending ?? false;
            if (readSlot == null) Debugger?.AddLine($"{Monitor.Id} skipped");

            if (readSlot == null && !ExtFormNeedsSync) return;

            if (isCabEnabled && D3D9MergedTexture != null && readSlot?.ProjectedD3D9Texture != null)
                using (var srcSurface = readSlot.ProjectedD3D9Texture.GetSurfaceLevel(0))
                using (var dstSurface = D3D9MergedTexture.GetSurfaceLevel(0))
                {
                    Direct3DProvider.Instance.Device.StretchRectangle(srcSurface, dstSurface,
                        TextureFilter.None);
                    Debugger?.AddLine($"{Monitor.Id} sync");
                }

            if (hasExtForm)
                if (Actions.ShouldSyncExternal || isTransition || ExtFormNeedsSync)
                {
                    SyncExternalCopy(MonitorGameOutput.OutputD3D11Texture);
                    Debugger?.AddLine($"{Monitor.Id} sync external");
                }
        }

        public override void Detach()
        {
            base.Detach();
            _sharedBuffer?.Reset();
            _hasPendingAcquisition = false;
            UpdateRenderTargets();
        }

        public override void DisposeD3D9Texture()
        {
            base.DisposeD3D9Texture();
            _sharedBuffer?.DisposeD3D9TexturesOnly();
        }

        /// <summary>
        ///     热重载 BufferFrameCount：重建 D3D9ExSharedBuffer 与 D3D9 纹理，重置 Sync 状态机。
        ///     CreateCabD3D11Texture 会 Dispose 旧 _sharedBuffer 并按新 BufferFrameCount 重建，
        ///     状态机天然回到干净初值（新建 SharedSurfaceQueue display slot = -1）。
        /// </summary>
        public override void ReconfigureBufferFrameCount(int newBufferFrameCount)
        {
            BufferFrameCount = newBufferFrameCount;
            // 释放旧 D3D9 纹理（CreateCabD3D11Texture 内部会重建 _sharedBuffer 并重绑 CabD3D11Texture）
            DisposeD3D9Texture();
            D3D9MergedTexture?.Dispose();
            D3D9MergedTexture = null;
            D3D9TextureRecreated = true;
            CreateCabD3D11Texture();
            EnsureD3D9Texture();
            _hasPendingAcquisition = false;
            UpdateRenderTargets();
            IsCabProjectionDirty = true;
        }

        public override void Dispose()
        {
            _sharedBuffer?.Dispose();
            _sharedBuffer = null;
            base.Dispose();
        }
    }
}