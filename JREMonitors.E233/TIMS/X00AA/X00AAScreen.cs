using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.X00AA
{
    public class X00AAScreen : Screen
    {
        public X00AAScreen(RenderContext context, TIMSVehicleSpec spec) : base(ScreenIds.X00AA,
            ScreenSizes.TIMSScreenSize, RefreshSpeeds.Fast, true)
        {
            BackgroundRoot = new X00AABackgroundRoot(context);
            ForegroundRoot = new X00AAForegroundRoot(context, spec);
        }

        public override Color4 BackgroundColor => MonitorColors.TIMSScreenBackground;
    }
}