using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCommonButtonGroup : Widget
    {
        public TIMSCommonButtonGroup(RenderContext context) : base(context)
        {
            var s00abButton = new TIMSButton(context, new Vector2(744, 4), LayoutLength.Absolute(54),
                LayoutLength.Absolute(54),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer(this.CreateTIMSTextLayout("初期\n選択", fixedLineSpacing: 2),
                    horizontalAlignment: 0.5f, verticalAlignment: 0.5f), reboundImmediate: true);
            s00abButton.OnClick += OnS00ABButtonOnClick;
            AddChild(s00abButton);
            AddChild(new TIMSVolumeButton(context, new Vector2(704, 506), LayoutLength.Absolute(35),
                LayoutLength.Absolute(72)));
            AddChild(new TIMSBrightnessButton(context, new Vector2(754, 506), LayoutLength.Absolute(35),
                LayoutLength.Absolute(72)));
        }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        private void OnS00ABButtonOnClick()
        {
            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.S00AB); });
        }
    }
}