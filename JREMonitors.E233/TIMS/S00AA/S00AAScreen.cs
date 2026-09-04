using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.S00AA
{
    public class S00AAScreen : Screen
    {
        public S00AAScreen(RenderContext context) : base(ScreenIds.S00AA, ScreenSizes.TIMSScreenSize,
            RefreshSpeeds.Fast, true)
        {
            BackgroundRoot = new S00AABackgroundRoot(context);
        }

        public override Color4 BackgroundColor => MonitorColors.TIMSScreenBackground;
    }
}