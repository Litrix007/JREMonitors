using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Foreground;

namespace JREMonitors.E233.TidScreen
{
    public class TidScreen1000 : Screen
    {
        public TidScreen1000(RenderContext context) : base(
            ScreenIds.Tid,
            ScreenSizes.TidScreenSize,
            RefreshSpeeds.Fast,
            true,
            MonitorColors.TidScreenBackground
        )
        {
            ForegroundRoot = new TidForegroundRoot1000(context);
        }
    }
}