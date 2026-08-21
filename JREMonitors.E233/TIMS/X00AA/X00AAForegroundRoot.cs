using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.X00AA
{
    public class X00AAForegroundRoot : Widget
    {
        private const float ConfirmButtonWidth = 220;
        private const float ConfirmButtonHeight = 100;

        public X00AAForegroundRoot(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            AddChild(new TIMSScreenTitle(context, ScreenIds.X00AA, "番台確認"));
            var confirmButton = new TIMSButton(context,
                new Vector2(400 - ConfirmButtonWidth / 2, 300 - ConfirmButtonHeight / 2),
                LayoutLength.Absolute(ConfirmButtonWidth), LayoutLength.Absolute(ConfirmButtonHeight),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer(spec.VehicleName, 2, 2, 0.5f, 0.5f,
                    useVerticalOverhangMetrics: true));
            confirmButton.OnClick += OnConfirmButtonClick;
            AddChild(confirmButton);
        }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private void OnConfirmButtonClick()
        {
            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.S00AB); });
        }
    }
}