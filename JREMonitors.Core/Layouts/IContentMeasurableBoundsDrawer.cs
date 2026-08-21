using System.Drawing;

namespace JREMonitors.Core.Layouts
{
    public interface IContentMeasurableBoundsDrawer : IBoundsDrawer
    {
        RectangleF GetContentBounds(RectangleF targetBounds);
    }
}