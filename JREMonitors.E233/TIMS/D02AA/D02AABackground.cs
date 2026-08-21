using System.Drawing;
using JREMonitors.Core.Contexts;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D02AA
{
    public class D02AABackground : TIMSCommonBackground
    {
        public D02AABackground(RenderContext context) : base(context)
        {
        }


        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            Context.CommonBrush.Color = Colors.Black;
            Context.DeviceContext.FillRectangle(new RectangleF(0, 250, 800, 230), Context.CommonBrush);
        }
    }
}