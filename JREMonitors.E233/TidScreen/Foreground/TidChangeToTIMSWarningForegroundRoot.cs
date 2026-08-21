using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public class TidChangeToTIMSWarningForegroundRoot : Widget
    {
        private const float ButtonWidth = 100;
        private const float ButtonHeight = 50;
        private const float PaddingBottom = 10;

        public TidChangeToTIMSWarningForegroundRoot(RenderContext context) : base(context)
        {
            var backButton = new TIMSButton(context,
                new Vector2(400 - ButtonWidth / 2, 599 - PaddingBottom - ButtonHeight),
                LayoutLength.Absolute(ButtonWidth), LayoutLength.Absolute(ButtonHeight),
                Context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("戻\u3000る", horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                    useVerticalOverhangMetrics: true));
            backButton.OnClick += OnBackButtonClick;
            AddChild(backButton);
            var confirmButton = new TIMSButton(context,
                new Vector2(799 - ButtonWidth, 599 - PaddingBottom - ButtonHeight),
                LayoutLength.Absolute(ButtonWidth), LayoutLength.Absolute(ButtonHeight),
                Context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("確\u3000認", horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                    useVerticalOverhangMetrics: true),
                reboundImmediate: true);
            confirmButton.OnClick += OnConfirmButtonClick;
            AddChild(confirmButton);
        }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private void OnBackButtonClick()
        {
            Context.DisplayController.RequestChangeScreen(ScreenIds.Tid);
        }

        private void OnConfirmButtonClick()
        {
            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.S00AB); });
        }
    }
}