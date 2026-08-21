using System;
using System.Drawing;
using System.Threading;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils.Render;
using SlimDX.Direct3D9;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Format = SlimDX.Direct3D9.Format;
using QueryType = Vortice.Direct3D11.QueryType;
using Usage = SlimDX.Direct3D9.Usage;

namespace JREMonitors.BveEx.Monitors
{
    public class D3D9ExSharedBuffer : IDisposable
    {
        private readonly Size _cabSize;
        private readonly int _capacity;

        private readonly MonitorContext _context;
        private readonly bool _isCabEnabled;
        private readonly SurfaceSlot[] _slots;
        private bool _isFirstFrame = true;

        private int _writeIndex;

        public D3D9ExSharedBuffer(MonitorContext context, bool isCabEnabled,
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

        public void EnsureD3D9Textures(Device device)
        {
            if (!_isCabEnabled) return;
            foreach (var slot in _slots)
                if (slot.ProjectedD3D9Texture == null)
                {
                    var handle = slot.SharedHandle;
                    slot.ProjectedD3D9Texture = new Texture(device, _cabSize.Width, _cabSize.Height, 1,
                        Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, ref handle);
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
        ///     D3D11 背压判定：下一个写槽（即将被覆写的槽位）上一次提交的 GPU 工作是否仍未完成。
        ///     pending ⇒ GPU 已落后 ≥ capacity 帧提交 ⇒ D3D11 队列积压，需主动背压。
        ///     同时这也是覆写安全检查：pending 时该槽位禁止覆写。
        /// </summary>
        public bool IsNextWriteSlotPending()
        {
            if (!_isCabEnabled) return false;
            var slot = _slots[_writeIndex];
            if (!slot.HasBeenSubmitted || slot.FenceQuery == null) return false;
            var available =
                _context.D3D11Context.GetData(slot.FenceQuery, AsyncGetDataFlags.DoNotFlush, out int done);
            return !(available && done != 0);
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
            foreach (var slot in _slots) slot.IsSubmitted = false;
        }

        public void PrepareTransition()
        {
            _isFirstFrame = true;
        }

        public void DisposeD3D9TexturesOnly()
        {
            foreach (var slot in _slots) slot.DisposeD3D9TextureOnly();
        }

        public class SurfaceSlot : IDisposable
        {
            public ID3D11Query FenceQuery;

            /// <summary>
            ///     曾经被提交过（fence 至少 End 过一次）。背压判定用；Reset() 不清除，fence 状态跨 Reset 仍有效。
            /// </summary>
            public bool HasBeenSubmitted;

            public bool IsSubmitted;
            public ID2D1Bitmap ProjectedD2D1Bitmap;
            public ID3D11Texture2D ProjectedD3D11Texture;
            public Texture ProjectedD3D9Texture;
            public IntPtr SharedHandle = IntPtr.Zero;

            public void Dispose()
            {
                FenceQuery?.Dispose();
                ProjectedD2D1Bitmap?.Dispose();
                ProjectedD3D11Texture?.Dispose();
                DisposeD3D9TextureOnly();
            }

            public void DisposeD3D9TextureOnly()
            {
                ProjectedD3D9Texture?.Dispose();
                ProjectedD3D9Texture = null;
            }
        }
    }
}