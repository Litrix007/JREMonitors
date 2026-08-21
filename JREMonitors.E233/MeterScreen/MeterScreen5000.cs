using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Foreground;

namespace JREMonitors.E233.MeterScreen
{
    public class MeterScreen5000 : Screen
    {
        public MeterScreen5000(RenderContext context) : base(
            ScreenIds.Meter,
            ScreenSizes.MeterScreenSize,
            RefreshSpeeds.Slow,
            true,
            MonitorColors.MeterScreenBackground,
            false
        )
        {
            BackgroundRoot = new MeterBackgroundRoot5000(context);
            ForegroundRoot = new MeterForegroundRoot5000(context);
        }
    }
}