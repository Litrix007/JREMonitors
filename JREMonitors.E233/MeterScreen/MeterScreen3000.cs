using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Foreground;

namespace JREMonitors.E233.MeterScreen
{
    public class MeterScreen3000 : Screen
    {
        public MeterScreen3000(RenderContext context) : base(
            ScreenIds.Meter,
            ScreenSizes.MeterScreenSize,
            RefreshSpeeds.Slow,
            true,
            MonitorColors.MeterScreenBackground,
            false
        )
        {
            BackgroundRoot = new MeterBackgroundRoot3000(context);
            ForegroundRoot = new MeterForegroundRoot3000(context);
        }
    }
}