using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Foreground;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen
{
    public class MeterScreen0 : Screen
    {
        public MeterScreen0(RenderContext context) : base(
            ScreenIds.Meter,
            ScreenSizes.MeterScreenSize,
            RefreshSpeeds.Fast,
            true,
            false
        )
        {
            BackgroundRoot = new MeterBackgroundRoot0(context);
            ForegroundRoot = new MeterForegroundRoot0(context);
        }

        public override Color4 BackgroundColor => MonitorColors.MeterScreenBackground;
    }
}