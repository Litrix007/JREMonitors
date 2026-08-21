using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.S00AA
{
    public class S00AAScreen : Screen
    {
        public S00AAScreen(RenderContext context) : base(ScreenIds.S00AA, ScreenSizes.TIMSScreenSize,
            RefreshSpeeds.Fast, true, MonitorColors.TIMSScreenBackground)
        {
            BackgroundRoot = new S00AABackgroundRoot(context);
        }
    }
}