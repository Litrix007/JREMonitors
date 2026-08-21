using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AXBackground : TIMSCommonBackground
    {
        public C01AXBackground(RenderContext context) : base(context)
        {
            BlackBounds = CreatePropertySlot<RectangleF>(DirtyType.Visual);
        }

        public PropertySlot<RectangleF> BlackBounds { get; }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            Context.CommonBrush.Color = Colors.Black;
            Context.DeviceContext.FillRectangle(BlackBounds, Context.CommonBrush);
        }
    }
}