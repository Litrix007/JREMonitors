using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.E233.TidScreen.LampGroups;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidForegroundRoot1000 : TidForegroundRootBase
    {
        public TidForegroundRoot1000(RenderContext context) : base(context)
        {
            AddChild(new TidNormalAtcLampGroup(context, 10, true));
            AddChild(new TidTascWideLampGroup(context, 767, 1));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}