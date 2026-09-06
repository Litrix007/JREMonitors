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
            // 0 = 未配置哨兵 → 默认 MaxFrameLatency + 2：D3D9Ex 侧稳态无背压停产需 capacity ≥ MaxFrameLatency + 2，
            return MathHelper.Clamp(
                raw == 0 ? ((DeviceEx)Direct3DProvider.Instance.Device).MaximumFrameLatency + 2 : raw, 3, 7);
        }
    }

    public class MonitorHolderD3D9Ex : MonitorHolderBase
    {
        private D3D9ExBufferQueue _bufferQueue;
        private bool _hadConsumers;
        private bool _hasPendingAcquisition;

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
            _bufferQueue?.Dispose();
            _bufferQueue = new D3D9ExBufferQueue(Context, Properties.Cab.Enabled, cabSize,
                BufferFrameCount);
            _hadConsumers = true;
            UpdateRenderTargets();
        }

        /// <summary>
        ///     写槽背压：下一个写槽的 D3D11 写侧 fence（该设备只渲染到纹理、无 Present 节流点，队列可无界增长）
        ///     或 D3D9Ex 读侧 fence（读完成受游戏 MaxFrameLatency 制约，读查询最晚在 CPU 领先 MFL 帧后才完成）
        ///     未完成时停产，画面保持上一完整帧。
        ///     稳态无停产需 BufferFrameCount ≥ MaxFrameLatency + 2（MaxFrameLatency=1..5 ⇒ 3..7，
        ///     clamp 上限 7 与游戏侧 MaxFrameLatency 可配置范围 1~5 对应）。
        /// </summary>
        protected override bool IsProductionBlocked()
        {
            return _bufferQueue != null && _bufferQueue.IsNextWriteSlotPending();
        }

        private void UpdateRenderTargets()
        {
            if (_bufferQueue == null) return;
            var writeSlot = _bufferQueue.AcquireWriteSlot();
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

            _bufferQueue?.EnsureD3D9ExTextures(Direct3DProvider.Instance.Device);
        }

        public override void Submit()
        {
            if (!Actions.ShouldSubmit) return;
            if (!Properties.Cab.Enabled && (ExternalForm == null || !ExternalForm.IsVisible)) return;
            Submitted = true;
            _bufferQueue?.SubmitWriteSlot();
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
                _bufferQueue?.PrepareTransition();
                _hadConsumers = true;
            }

            if (!Actions.ShouldSync && !isTransition && !D3D9TextureRecreated && !_hasPendingAcquisition &&
                !ExtFormNeedsSync) return;
            D3D9TextureRecreated = false;
            var readSlot = _bufferQueue?.TryAcquireDisplaySlot();
            _hasPendingAcquisition = _bufferQueue?.HasNewerPending ?? false;
            if (readSlot == null) Debugger?.AddLine($"{Monitor.Id} skipped");
            if (readSlot == null && !ExtFormNeedsSync) return;

            if (isCabEnabled && D3D9MergedTexture != null && readSlot?.ProjectedD3D9ExTexture != null)
                using (var srcSurface = readSlot.ProjectedD3D9ExTexture.GetSurfaceLevel(0))
                using (var dstSurface = D3D9MergedTexture.GetSurfaceLevel(0))
                {
                    Direct3DProvider.Instance.Device.StretchRectangle(srcSurface, dstSurface,
                        TextureFilter.None);
                    // 读取 fence 必须紧跟 StretchRect 之后 Issue：查询完成点 = GPU 通过该读取点
                    _bufferQueue?.MarkD3D9ExReadIssued(readSlot);
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
            _bufferQueue?.Reset();
            _hasPendingAcquisition = false;
            UpdateRenderTargets();
        }

        public override void DisposeD3D9Texture()
        {
            base.DisposeD3D9Texture();
            _bufferQueue?.DisposeD3D9ExResourcesOnly();
        }

        /// <summary>
        ///     热重载 BufferFrameCount：重建 D3D9ExBufferQueue 与 D3D9Ex 侧纹理，重置 Sync 状态机。
        ///     CreateCabD3D11Texture 会 Dispose 旧 _bufferQueue 并按新 BufferFrameCount 重建，
        ///     状态机天然回到干净初值（新建 D3D9ExBufferQueue：_writeIndex=0、全部槽位未提交）。
        /// </summary>
        public override void ReconfigureBufferFrameCount(int newBufferFrameCount)
        {
            BufferFrameCount = newBufferFrameCount;
            // 释放旧 D3D9Ex 侧纹理（CreateCabD3D11Texture 内部会重建 _bufferQueue 并重绑 CabD3D11Texture）
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
            _bufferQueue?.Dispose();
            _bufferQueue = null;
            base.Dispose();
        }
    }
}