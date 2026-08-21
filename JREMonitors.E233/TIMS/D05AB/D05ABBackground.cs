using System.Drawing;
using JREMonitors.Core.Contexts;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABBackground : TIMSCommonBackground
    {
        public D05ABBackground(RenderContext context) : base(context)
        {
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            Context.CommonBrush.Color = Colors.Black;
            Context.DeviceContext.FillRectangle(new RectangleF(0, D05ABDataTable.TableY, 800, 250),
                Context.CommonBrush);
        }
    }
}