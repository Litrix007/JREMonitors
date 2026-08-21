using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;

namespace JREMonitors.Core.Layouts
{
    public class PlaceHolder : Widget, ILayoutable
    {
        public PlaceHolder(RenderContext context, LayoutLength? width = null, LayoutLength? height = null) :
            base(context)
        {
            PreferredWidth = width ?? LayoutLength.Flex();
            PreferredHeight = height ?? LayoutLength.Flex();
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public LayoutLength PreferredWidth { get; }

        public LayoutLength PreferredHeight { get; }

        public float MarginWidth => 0;
        public float MarginHeight => 0;
        public bool SkipArrangeWhenHidden => true;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;

        public void SetLayoutSize(float width, float height)
        {
        }
    }
}