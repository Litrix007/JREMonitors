using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Foreground;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen
{
    public class TidScreen1000 : Screen
    {
        public TidScreen1000(RenderContext context) : base(
            ScreenIds.Tid,
            ScreenSizes.TidScreenSize,
            RefreshSpeeds.Fast,
            true
        )
        {
            ForegroundRoot = new TidForegroundRoot1000(context);
        }

        public override Color4 BackgroundColor => ((TidForegroundRoot1000)ForegroundRoot).BackgroundColor.Value;
    }
}