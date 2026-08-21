using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXScreen : Screen
    {
        public D01AXScreen(RenderContext context, TIMSVehicleSpec spec,
            D01AXTrainTypeButtonGroup trainTypeButtonGroup) : base(ScreenIds.D01AX,
            ScreenSizes.TIMSScreenSize, 0, true, Colors.Transparent)
        {
            ForegroundRoot = new D01AXForegroundRoot(context, spec, trainTypeButtonGroup);
        }
    }
}