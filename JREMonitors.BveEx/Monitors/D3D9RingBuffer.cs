using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct3D11;

namespace JREMonitors.BveEx.Monitors
{
    public class D3D9RingBuffer : RingBufferBase
    {
        private readonly MonitorContext _context;
        private readonly bool _isCabEnabled;
        private readonly BufferSlot[] _slots;

        public D3D9RingBuffer(MonitorContext context, bool isCabEnabled, Size cabSize, int length) :
            base(length)
        {
            _context = context;
            _isCabEnabled = isCabEnabled;
            _slots = new BufferSlot[Length];

            for (var i = 0; i < Length; i++)
            {
                _slots[i] = new BufferSlot();
                if (_isCabEnabled)
                    _slots[i].StagingProjectedTexture =
                        context.D3D11Device.CreateTexture2D(
                            RenderHelper.CreateStagingTextureDescription(cabSize.Width, cabSize.Height));
            }

            Reset();
        }

        public void Submit(ID3D11Texture2D sourceProjectedTexture)
        {
            if (_isCabEnabled && sourceProjectedTexture != null)
                _context.D3D11Context.CopyResource(_slots[WriteIndex].StagingProjectedTexture, sourceProjectedTexture);

            WriteIndex = (WriteIndex + 1) % Length;
            UnmappedCount = Math.Min(Length, UnmappedCount + 1);
            SubmittedThisFrame = true;
        }

        public MappedSubresource Map(out BufferSlot slot)
        {
            if (ActiveMapIndex != -1)
                throw new InvalidOperationException("Map is already active on this buffer. Call Unmap first.");

            var targetMapIndex = ResolveReadIndex();
            slot = _slots[targetMapIndex];
            ActiveMapIndex = targetMapIndex;

            if (slot.StagingProjectedTexture != null) return _context.D3D11Context.Map(slot.StagingProjectedTexture, 0);

            return default;
        }

        public void Unmap()
        {
            if (ActiveMapIndex == -1) return;

            var slot = _slots[ActiveMapIndex];
            if (slot.StagingProjectedTexture != null)
                _context.D3D11Context.Unmap(slot.StagingProjectedTexture, 0);

            ActiveMapIndex = -1;
        }

        public override void Dispose()
        {
            if (_slots != null)
                for (var i = 0; i < _slots.Length; i++)
                    _slots[i]?.Dispose();
        }

        public class BufferSlot : IDisposable
        {
            public ID3D11Texture2D StagingProjectedTexture;

            public void Dispose()
            {
                StagingProjectedTexture?.Dispose();
                StagingProjectedTexture = null;
            }
        }
    }
}