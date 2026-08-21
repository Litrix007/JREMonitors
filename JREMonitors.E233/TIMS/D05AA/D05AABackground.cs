using System.Drawing;
using JREMonitors.Core.Contexts;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D05AA
{
    public class D05AABackground : TIMSCommonBackground
    {
        public D05AABackground(RenderContext context) : base(context)
        {
        }


        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            Context.CommonBrush.Color = Colors.Black;
            Context.DeviceContext.FillRectangle(new RectangleF(0, 160, 800, 290), Context.CommonBrush);
        }
    }
}