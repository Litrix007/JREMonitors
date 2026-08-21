using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class MrBackground : Widget
    {
        public const float Width = 50;
        public const float Height = BcBackground.Height;
        protected const float VerticalTickMarksOffsetX = Width + 28;
        private static readonly RectangleF MainBounds = new RectangleF(0, 0, Width, Height);

        private static readonly RectangleF MrTitleBounds =
            new RectangleF(0, VerticalTickMarks.TitleOffsetTop, 70, -VerticalTickMarks.TitleOffsetTop);

        private readonly Baker _kpaTitleBaker;
        private readonly Baker _mrTitleBaker;
        private readonly IDWriteTextFormat _titleFormat;

        public MrBackground(RenderContext context, float x, float y) : base(context, x, y)
        {
            MainBaker = new Baker(context);
            RegisterResource(MainBaker);
            _mrTitleBaker = new Baker(context);
            RegisterResource(_mrTitleBaker);
            _kpaTitleBaker = new Baker(context);
            RegisterResource(_kpaTitleBaker);
            _titleFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, FontSizes.Title,
                    fontWeight: FontWeight.SemiBold);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private static RectangleF KpaTitleBounds
        {
            get
            {
                var bounds = MrTitleBounds;
                bounds.Offset(VerticalTickMarksOffsetX, 0);
                return bounds;
            }
        }

        protected bool ShouldDrawTitle { get; set; } = true;
        protected Baker MainBaker { get; }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            MainBaker.BakeAndDraw(MainBounds, DrawMain);
            if (ShouldDrawTitle)
            {
                _mrTitleBaker.BakeAndDraw(MrTitleBounds, DrawMrTitle);
                _kpaTitleBaker.BakeAndDraw(KpaTitleBounds, DrawKpaTitle);
            }
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            MainBaker.BakeAndDraw(MainBounds, DrawMain);
            if (ShouldDrawTitle)
            {
                _mrTitleBaker.BakeAndDraw(MrTitleBounds, DrawMrTitle);
                _kpaTitleBaker.BakeAndDraw(KpaTitleBounds, DrawKpaTitle);
            }
        }

        private void DrawMrTitle()
        {
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop, () =>
            {
                Context.CommonBrush.Color = MonitorColors.White;
                Context.DeviceContext.DrawDynamicText(Context.DwFactory, "MR", Width / 2 + 3,
                    VerticalTickMarks.TitleOffsetBottom, _titleFormat, Context.CommonBrush, 0.5f, 1);
            });
        }

        private void DrawKpaTitle()
        {
            VerticalTickMarks.DrawTitle(VerticalTickMarksOffsetX, 0, Context);
        }

        private void DrawMain()
        {
            Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.RecessedInner, () =>
            {
                Context.CommonBrush.Color = MonitorColors.Recessed;
                Context.DeviceContext.FillRectangle(new RectangleF(PointF.Empty, new SizeF(Width, Height)),
                    Context.CommonBrush);
                OnCustomDraw();
            });
        }

        protected virtual void OnCustomDraw()
        {
        }
    }
}