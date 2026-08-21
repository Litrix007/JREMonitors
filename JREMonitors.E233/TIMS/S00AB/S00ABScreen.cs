using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.S00AB
{
    public class S00ABScreen : Screen
    {
        public S00ABScreen(RenderContext context, TIMSVehicleSpec spec) : base(ScreenIds.S00AB,
            ScreenSizes.TIMSScreenSize,
            RefreshSpeeds.Fast, true, MonitorColors.TIMSScreenBackground)
        {
            ForegroundRoot = new S00ABForegroundRoot(context, spec);
        }
    }
}