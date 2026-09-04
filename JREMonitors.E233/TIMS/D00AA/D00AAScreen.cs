using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D00AA
{
    public class D00AAScreen : Screen
    {
        public D00AAScreen(RenderContext context, D00AAButtonGroup buttonGroup) : base(ScreenIds.D00AA,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new D00AAForegroundRoot(context, buttonGroup);
        }
    }
}