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
            string tidScreenWithSafetyLampsName, string meterScreenWithSafetyLampsWithoutTascName = null,
            string tidScreenWithSafetyLampsWithoutTascName = null) : base(ScreenIds.TidChangeToTIMSWarning,
            ScreenSizes.TIMSScreenSize, RefreshSpeeds.Fast, true, false)
        {
            BackgroundRoot = new TidChangeToTIMSWarningBackgroundRoot(context, meterScreenWithSafetyLampsName,
                tidScreenWithSafetyLampsName, meterScreenWithSafetyLampsWithoutTascName,
                tidScreenWithSafetyLampsWithoutTascName);
            ForegroundRoot = new TidChangeToTIMSWarningForegroundRoot(context);
        }

        public override Color4 BackgroundColor => Colors.Black;
    }
}