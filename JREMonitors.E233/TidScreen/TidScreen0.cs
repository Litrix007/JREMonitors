using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Foreground;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen
{
    public class TidScreen0 : Screen
    {
        public TidScreen0(RenderContext context) : base(
            ScreenIds.Tid,
            ScreenSizes.TidScreenSize,
            RefreshSpeeds.Fast,
            true
        )
        {
            ForegroundRoot = new TidForegroundRoot0(context);
        }

        public override Color4 BackgroundColor => ((TidForegroundRoot0)ForegroundRoot).BackgroundColor.Value;
    }
}