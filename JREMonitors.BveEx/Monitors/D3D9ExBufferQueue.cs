using System;
using System.Drawing;
using System.Threading;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils.Render;
using SlimDX.Direct3D9;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using D3D9QueryType = SlimDX.Direct3D9.QueryType;
using Format = SlimDX.Direct3D9.Format;
using QueryType = Vortice.Direct3D11.QueryType;
using Usage = SlimDX.Direct3D9.Usage;

namespace JREMonitors.BveEx.Monitors
{
    public class D3D9ExBufferQueue : IDisposable
    {
        private readonly Size _cabSize;
        private readonly int _capacity;

        private readonly MonitorContext _context;
        private readonly bool _isCabEnabled;
        private readonly SurfaceSlot[] _slots;
        private bool _isFirstFrame = true;

        private int _writeIndex;

        public D3D9ExBufferQueue(MonitorContext context, bool isCabEnabled,
            Size cabSize,
            int capacity = 3)
        {
            if (capacity < 3) throw new ArgumentException("Capacity must be at least 3.", nameof(capacity));
            _context = context;
            _capacity = capacity;
            _isCabEnabled = isCabEnabled;
            _cabSize = cabSize;
            _slots = new SurfaceSlot[_capacity];

            var queryDesc = new QueryDescription
            {
                QueryType = QueryType.Event,
                MiscFlags = QueryFlags.None
            };

            for (var i = 0; i < _capacity; i++)
            {
                var slot = new SurfaceSlot();
                _slots[i] = slot;

                if (_isCabEnabled)
                {
                    var desc = RenderHelper.CreateRenderTargetTextureDescription(cabSize.Width, cabSize.Height);
                    desc.MiscFlags = ResourceOptionFlags.Shared;
                    slot.ProjectedD3D11Texture = context.D3D11Device.CreateTexture2D(desc);
                    using (var dxgiRes = slot.ProjectedD3D11Texture.QueryInterface<IDXGIResource>())
                    {
                        slot.SharedHandle = dxgiRes.SharedHandle;
                    }

                    using (var dxgiSurface = slot.ProjectedD3D11Texture.QueryInterface<IDXGISurface>())
                    {
                        slot.ProjectedD2D1Bitmap =
                            context.D2D1Context.CreateBitmapFromDxgiSurface(dxgiSurface,
                                RenderHelper.BitMapProperties8Bit);
                    }

                    slot.FenceQuery = context.D3D11Device.CreateQuery(queryDesc);
                }
            }
        }

        /// <summary>
        ///     是否存在已提交的槽位晚于已获取的槽位
        /// </summary>
        public bool HasNewerPending { get; private set; }

        public void Dispose()
        {
            foreach (var slot in _slots) slot.Dispose();
        }

        public void EnsureD3D9ExTextures(Device device)
        {
            if (!_isCabEnabled) return;
            foreach (var slot in _slots)
            {
                if (slot.ProjectedD3D9ExTexture == null)
                {
                    var handle = slot.SharedHandle;
                    slot.ProjectedD3D9ExTexture = new Texture(device, _cabSize.Width, _cabSize.Height, 1,
                        Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, ref handle);
                }

                if (slot.D3D9ExReadQuery == null)
                    slot.D3D9ExReadQuery = new Query(device, D3D9QueryType.Event);
            }
        }

        public SurfaceSlot AcquireWriteSlot()
        {
            return _slots[_writeIndex];
        }

        public void SubmitWriteSlot()
        {
            var slot = _slots[_writeIndex];
            if (_isCabEnabled && slot.FenceQuery != null)
                _context.D3D11Context.End(slot.FenceQuery);
            slot.IsSubmitted = true;
            slot.HasBeenSubmitted = true;
            _writeIndex = (_writeIndex + 1) % _capacity;
            _slots[_writeIndex].IsSubmitted = false;
        }

        /// <summary>
        ///     D3D9Ex 读取侧 fence：D3D9Ex 侧每次 StretchRectangle 读取 readSlot 后调用。
        ///     Issue(End) 以最新提交点重新武装 EVENT 查询——同槽连续帧被重读时，
        ///     CheckStatus 完成当且仅当 GPU 已通过最近一次读取点，语义仍正确。
        /// </summary>
        public void MarkD3D9ExReadIssued(SurfaceSlot slot)
        {
            if (slot?.D3D9ExReadQuery == null) return;
            slot.D3D9ExReadQuery.Issue(Issue.End);
            slot.HasD3D9ExReadPending = true;
        }

        /// <summary>
        ///     写槽覆写安全 + 双侧背压判定（下一个写槽 = 即将被覆写的槽位）：
        ///     1. D3D11 写侧：上一次写入该槽的 fence 未完成 ⇒ D3D11 队列积压或写入在途，禁止覆写；
        ///     2. D3D9Ex 读侧：上一次该槽被 D3D9Ex StretchRectangle 读取的查询未完成 ⇒ 读取在途，禁止覆写。
        ///     StretchRect 是 D3D9Ex 队列的异步命令（真正执行时刻由游戏命令流决定，可落后 CPU 数帧），
        ///     D3D11 fence 完成时刻对它零约束——缺读侧检查时覆写会与在途读取竞态（撕裂/整帧空白）。
        /// </summary>
        public bool IsNextWriteSlotPending()
        {
            if (!_isCabEnabled) return false;
            var slot = _slots[_writeIndex];

            if (slot.HasBeenSubmitted && slot.FenceQuery != null)
            {
                var available =
                    _context.D3D11Context.GetData(slot.FenceQuery, AsyncGetDataFlags.DoNotFlush, out int done);
                if (!(available && done != 0))
                    return true;
            }

            if (slot.HasD3D9ExReadPending && slot.D3D9ExReadQuery != null)
            {
                // flush=false：纯轮询，不冲刷游戏自己的 D3D9Ex 命令缓冲
                if (!slot.D3D9ExReadQuery.CheckStatus(false))
                    return true;
                slot.HasD3D9ExReadPending = false;
            }

            return false;
        }

        public SurfaceSlot TryAcquireDisplaySlot()
        {
            SurfaceSlot bestSlot = null;
            var foundAt = -1;
            var hasSubmitted = false;

            for (var i = 1; i < _capacity; i++)
            {
                var idx = (_writeIndex - i + _capacity) % _capacity;
                var slot = _slots[idx];
                if (!slot.IsSubmitted) continue;
                hasSubmitted = true;
                bool isDone;
                if (slot.FenceQuery != null)
                {
                    var available =
                        _context.D3D11Context.GetData(slot.FenceQuery, AsyncGetDataFlags.DoNotFlush, out int done);
                    isDone = available && done != 0;

                    if (!isDone && _isFirstFrame)
                    {
                        SpinWait sw = default;
                        while (!_context.D3D11Context.GetData(slot.FenceQuery, AsyncGetDataFlags.DoNotFlush,
                                   out done) || done == 0)
                            sw.SpinOnce();
                        isDone = true;
                    }
                }
                else
                {
                    isDone = true;
                }

                if (!isDone) continue;
                bestSlot = slot;
                foundAt = i;
                _isFirstFrame = false;

                for (var j = i + 1; j < _capacity; j++)
                {
                    var olderIdx = (_writeIndex - j + _capacity) % _capacity;
                    _slots[olderIdx].IsSubmitted = false;
                }

                break;
            }

            if (bestSlot == null)
            {
                // 只检查是否有未完成的槽位
                HasNewerPending = hasSubmitted;
            }
            else
            {
                // 检查是否有更新的未完成槽位
                HasNewerPending = false;
                for (var i = 1; i < foundAt; i++)
                {
                    var idx = (_writeIndex - i + _capacity) % _capacity;
                    if (_slots[idx].IsSubmitted)
                    {
                        HasNewerPending = true;
                        break;
                    }
                }
            }

            return bestSlot;
        }

        public void Reset()
        {
            _writeIndex = 0;
            _isFirstFrame = true;
            // 仅清除消费侧标记；HasBeenSubmitted / HasD3D9ExReadPending 保留——
            // GPU 在途的写/读不因 Reset 消失，跨 Detach/Attach 仍需背压保护。
            foreach (var slot in _slots) slot.IsSubmitted = false;
        }

        public void PrepareTransition()
        {
            _isFirstFrame = true;
        }

        public void DisposeD3D9ExResourcesOnly()
        {
            foreach (var slot in _slots) slot.DisposeD3D9ExResourcesOnly();
        }

        public class SurfaceSlot : IDisposable
        {
            /// <summary>
            ///     D3D9Ex 读完成查询：该槽每次被 D3D9Ex StretchRectangle 读取后 Issue(End)（见 MarkD3D9ExReadIssued）。
            ///     与 ProjectedD3D9ExTexture 同为 D3D9Ex 设备相关资源，设备丢失时随 DisposeD3D9ExResourcesOnly 释放重建。
            /// </summary>
            public Query D3D9ExReadQuery;

            public ID3D11Query FenceQuery;

            /// <summary>
            ///     曾经被提交过（fence 至少 End 过一次）。背压判定用；Reset() 不清除，fence 状态跨 Reset 仍有效。
            /// </summary>
            public bool HasBeenSubmitted;

            /// <summary>
            ///     最近一次 D3D9Ex 读取已 Issue、尚未被 CheckStatus 确认完成。写槽背压判定用；
            ///     Reset() 不清除（在途读取不因 Detach 消失），设备丢失重建时清除。
            /// </summary>
            public bool HasD3D9ExReadPending;

            public bool IsSubmitted;
            public ID2D1Bitmap ProjectedD2D1Bitmap;
            public ID3D11Texture2D ProjectedD3D11Texture;
            public Texture ProjectedD3D9ExTexture;

            public IntPtr SharedHandle = IntPtr.Zero;

            public void Dispose()
            {
                FenceQuery?.Dispose();
                ProjectedD2D1Bitmap?.Dispose();
                ProjectedD3D11Texture?.Dispose();
                DisposeD3D9ExResourcesOnly();
            }

            public void DisposeD3D9ExResourcesOnly()
            {
                D3D9ExReadQuery?.Dispose();
                D3D9ExReadQuery = null;
                HasD3D9ExReadPending = false;
                ProjectedD3D9ExTexture?.Dispose();
                ProjectedD3D9ExTexture = null;
            }
        }
    }
}