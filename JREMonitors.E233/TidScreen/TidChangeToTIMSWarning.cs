using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.Background;
using JREMonitors.E233.TidScreen.Foreground;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen
{
    public class TidChangeToTIMSWarningScreen : Screen
    {
        public TidChangeToTIMSWarningScreen(RenderContext context, string meterScreenWithSafetyLampsName,
            string tidScreenWithSafetyLampsName) : base(ScreenIds.TidChangeToTIMSWarning,
            ScreenSizes.TIMSScreenSize, RefreshSpeeds.Fast, true, Colors.Black)
        {
            BackgroundRoot = new TidChangeToTIMSWarningBackgroundRoot(context, meterScreenWithSafetyLampsName,
                tidScreenWithSafetyLampsName);
            ForegroundRoot = new TidChangeToTIMSWarningForegroundRoot(context);
        }
    }
}