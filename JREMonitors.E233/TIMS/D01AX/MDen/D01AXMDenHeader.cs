using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D01AX.MDen
{
    public class D01AXMDenHeader : Widget
    {
        private const float StartY = 112;

        public D01AXMDenHeader(RenderContext context) : base(context)
        {
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("時分"),
                contentColor: MonitorColors.White, x: 8, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("停車場名"),
                contentColor: MonitorColors.White, x: 135, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("着"),
                contentColor: MonitorColors.White, x: 328, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("発"),
                contentColor: MonitorColors.White, x: 535, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("番線"),
                contentColor: MonitorColors.White, x: 670, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("制限", horizontalAlignment: 1),
                contentColor: MonitorColors.White, x: 790, y: StartY));
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("次停車駅"),
                contentColor: MonitorColors.White, x: 95, y: 343));
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}