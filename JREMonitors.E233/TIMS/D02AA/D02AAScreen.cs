using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D02AA
{
    public class D02AAScreen : Screen
    {
        public D02AAScreen(RenderContext context, TIMSVehicleSpec spec) : base(ScreenIds.D02AA,
            ScreenSizes.TIMSScreenSize, 0, true, Colors.Transparent)
        {
            ForegroundRoot = new D02AAForegroundRoot(context, spec);
        }
    }
}