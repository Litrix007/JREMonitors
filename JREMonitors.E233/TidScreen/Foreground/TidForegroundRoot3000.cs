using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.TidScreen.LampGroups;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidForegroundRoot3000 : TidForegroundRootBase
    {
        public TidForegroundRoot3000(RenderContext context) : base(context)
        {
            var infoButtonGroup = CreateInfoButtonGroup(false);
            AddNonTascLayout(new Group(context, infoButtonGroup, new LampPanel(context, 1023, 1024,
                TidScreens.CompactLampHeight + 40, borderRadius: 0,
                clickable: false, children: new Widget[]
                {
                    infoButtonGroup.HomeButton,
                    new TidAtsLampGroup(context, 20, true, false)
                })));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}