using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.C00AA
{
    public class C00AAScreen : Screen
    {
        public C00AAScreen(RenderContext context, C00AAButtonGroup buttonGroup) : base(ScreenIds.C00AA,
            ScreenSizes.TIMSScreenSize, 0, true)
        {
            ForegroundRoot = new C00AAForegroundRoot(context, buttonGroup);
        }
    }
}