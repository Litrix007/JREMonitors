using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen.LampGroups;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidForegroundRoot0 : TidForegroundRootBase
    {
        public TidForegroundRoot0(RenderContext context) : base(context)
        {
            AddChild(new TidAtsLampGroup(context, 20, true, true));
            AddChild(new TidTascCompactLampGroup(context, 767 - 35,
                (768 - 20 - 35 - TidScreens.CompactLampHeight * 3) / 2, 1));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}