using System;

namespace JREMonitors.Core.DirtyUpdate
{
    public struct FrameDirtyReport
    {
        public ArraySegment<DirtyArea> ImmediateAreas;
        public ArraySegment<DirtyArea> DelayAreas;
        private bool _cleared;

        public void Clear()
        {
            if (_cleared) return;
            _cleared = true;
            var immediateAreas = ImmediateAreas;
            for (var i = 0; i < immediateAreas.Count; i++)
                if (immediateAreas.Array != null)
                    immediateAreas.Array[immediateAreas.Offset + i] = default;

            var delayAreas = DelayAreas;
            for (var i = 0; i < delayAreas.Count; i++)
                if (delayAreas.Array != null)
                    delayAreas.Array[delayAreas.Offset + i] = default;
        }
    }
}