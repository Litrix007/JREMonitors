using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidForegroundRootBase : Widget
    {
        protected readonly InfoButtonGroup InfoButtonGroup;

        public TidForegroundRootBase(RenderContext context, bool addHomeButton = true) : base(context)
        {
            InfoButtonGroup = new InfoButtonGroup(context, 10, new Vector2(1024, 768), addHomeButton);
            InfoButtonGroup.HomeButton.OnClick += OnHomeButtonClick;
            AddChild(InfoButtonGroup);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private void OnHomeButtonClick()
        {
            Context.DisplayController.RequestChangeScreen(ScreenIds.TidChangeToTIMSWarning);
        }
    }
}