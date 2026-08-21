using System.Collections.Generic;

namespace JREMonitors.Core.DirtyUpdate
{
    public class FrameCollector : IDirtyReporter
    {
        private const int InitialCapacity = 64;
        public List<DirtyArea> Immediate { get; } = new List<DirtyArea>(InitialCapacity);
        public List<DirtyArea> Delay { get; } = new List<DirtyArea>(InitialCapacity);

        public void ReportArea(DirtyArea area)
        {
            if (area.RefreshSpeed > 0)
                Delay.Add(area);
            else
                Immediate.Add(area);
        }

        public void Clear()
        {
            Immediate.Clear();
            Delay.Clear();
        }
    }
}