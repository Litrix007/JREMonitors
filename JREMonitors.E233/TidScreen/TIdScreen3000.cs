using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Foreground;

namespace JREMonitors.E233.TidScreen
{
    public class TidScreen3000 : Screen
    {
        public TidScreen3000(RenderContext context) : base(
            ScreenIds.Tid,
            ScreenSizes.TidScreenSize,
            RefreshSpeeds.Fast,
            true,
            MonitorColors.TidScreenBackground
        )
        {
            ForegroundRoot = new TidForegroundRoot3000(context);
        }
    }
}