using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABScreen : Screen
    {
        public D05ABScreen(RenderContext context, TIMSVehicleSpec spec, D05ABDataTable dataTable = null) : base(
            ScreenIds.D05AB,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new D05ABForegroundRoot(context, spec, dataTable ?? new D05ABDataTable(context, spec));
        }
    }
}