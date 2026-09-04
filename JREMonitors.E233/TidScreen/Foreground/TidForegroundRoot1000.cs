using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.TidScreen.LampGroups;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidForegroundRoot1000 : TidForegroundRootBase
    {
        public TidForegroundRoot1000(RenderContext context) : base(context)
        {
            AddTascLayout(CreateTascLayout(context));
            AddNonTascLayout(CreateNonTascLayout(context));
            BackgroundColor = CreateComputed(() =>
                ViewModel.SupportsTasc ? MonitorColors.PanelColor : MonitorColors.TidScreenBackground);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public Computed<Color4> BackgroundColor { get; }

        private Widget CreateTascLayout(RenderContext context)
        {
            return new Group(context,
                CreateInfoButtonGroup(),
                new TidNormalAtcLampGroup(context, 10, true),
                new TidTascWideLampGroup(context, 767, 1));
        }

        private Widget CreateNonTascLayout(RenderContext context)
        {
            var infoButtonGroup = CreateInfoButtonGroup(false);
            return new Group(context, infoButtonGroup, new LampPanel(context, 1023, 1024,
                20 + TidScreens.WideLampHeight * 2 + TidScreens.WideRowSpacing,
                borderRadius: 0,
                clickable: false, children: new Widget[]
                {
                    infoButtonGroup.HomeButton,
                    new TidNormalAtcLampGroup(context, 10, true)
                }));
        }
    }
}