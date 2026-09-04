using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Foreground;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen
{
    public class TidScreen3000 : Screen
    {
        public TidScreen3000(RenderContext context) : base(
            ScreenIds.Tid,
            ScreenSizes.TidScreenSize,
            RefreshSpeeds.Fast,
            true
        )
        {
            ForegroundRoot = new TidForegroundRoot3000(context);
        }

        public override Color4 BackgroundColor => MonitorColors.TidScreenBackground;
    }
}