using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXBackground : TIMSCommonBackground
    {
        public D01AXBackground(RenderContext context) : base(context)
        {
            TimetableBounds = CreatePropertySlot<RectangleF>(DirtyType.Visual);
        }

        public PropertySlot<RectangleF> TimetableBounds { get; }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            if (TimetableBounds.Value.IsEmpty) return;
            Context.CommonBrush.Color = "#3F454D".ToColor4();
            Context.DeviceContext.FillRectangle(TimetableBounds, Context.CommonBrush);
        }
    }
}