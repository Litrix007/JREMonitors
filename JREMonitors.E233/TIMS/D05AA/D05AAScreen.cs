using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AA
{
    public class D05AAScreen : Screen
    {
        public D05AAScreen(RenderContext context, TIMSVehicleSpec spec, D05AABrakeInformation brakeInformation) : base(
            ScreenIds.D05AA,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new D05AAForegroundRoot(context, spec, brakeInformation);
        }
    }
}