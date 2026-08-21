using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.X00AA
{
    public class X00AABackgroundRoot : Widget
    {
        public const float Padding = 5;

        public X00AABackgroundRoot(RenderContext context) : base(context)
        {
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("JREMonitors developed by Litrix", verticalAlignment: 1),
                contentColor: MonitorColors.White, x: Padding,
                y: 599 - Padding));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}