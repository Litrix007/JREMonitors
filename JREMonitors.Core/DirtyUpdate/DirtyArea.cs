using System.Drawing;

namespace JREMonitors.Core.DirtyUpdate
{
    public readonly struct DirtyArea
    {
        public readonly RectangleF Rect;
        public readonly float RefreshSpeed;

        public DirtyArea(RectangleF rect, float refreshSpeed)
        {
            Rect = rect;
            RefreshSpeed = refreshSpeed;
        }
    }
}