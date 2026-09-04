using System.Drawing;
using JREMonitors.Core.Contexts;

namespace JREMonitors.Core.Widgets
{
    public class Group : Widget
    {
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public Group(RenderContext context, params Widget[] children) : this(context, 0, 0, children)
        {
        }

        public Group(RenderContext context, float x = 0, float y = 0, params Widget[] children) : base(context, x, y)
        {
            if (children == null) return;
            foreach (var child in children) AddChild(child);
        }

        public new void AddChild(Widget widget, bool isGlobalPosition = false)
        {
            base.AddChild(widget, isGlobalPosition);
        }

        public new void InsertChild(Widget widget, int index = 0, bool isGlobalPosition = false)
        {
            base.InsertChild(widget, index, isGlobalPosition);
        }

        public new void InsertChildAfter(Widget widget, Widget afterWidget, bool isGlobalPosition = false)
        {
            base.InsertChildAfter(widget, afterWidget, isGlobalPosition);
        }
    }
}