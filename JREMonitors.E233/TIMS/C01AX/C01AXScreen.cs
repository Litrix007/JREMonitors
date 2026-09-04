using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AAScreen : Screen
    {
        public C01AAScreen(RenderContext context, TIMSVehicleSpec spec) : base(ScreenIds.C01AA,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new C01AAForegroundRoot(context, spec);
        }
    }

    public class C01ABScreen : Screen
    {
        public C01ABScreen(RenderContext context, TIMSVehicleSpec spec) : base(ScreenIds.C01AB,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new C01ABForegroundRoot(context, spec);
        }
    }
}