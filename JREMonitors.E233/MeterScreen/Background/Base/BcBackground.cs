using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class BcBackground : Widget
    {
        public const float Height = 397;
        public const int ShortWidth = 51;
        public const int LongWidth = 106;
        public const int TopMajorTickMarkWidth = 25;
        public const int BottomMajorTickMarkWidth = 25 + LongWidth - ShortWidth;
        public const float DividerCount = 3;
        public const float DividerStrokeWidth = 3;
        public const int MaxBc = 800;

        private static readonly RectangleF BakeBounds =
            RectangleF.FromLTRB(0, VerticalTickMarks.TitleOffsetTop, LongWidth, Height);

        private readonly Baker _baker;
        private readonly ID2D1PathGeometry _geometry;
        private readonly IDWriteTextFormat _titleFormat;
        private readonly VerticalTickMarks _verticalTickMarks;


        public BcBackground(RenderContext context, float x, float y) : base(context, x, y)
        {
            _geometry = CreateBcTrapezoidGeometry(context.D2D1Factory);
            RegisterResource(_geometry);
            _verticalTickMarks = new VerticalTickMarks(context, LongWidth + 28, 0)
            {
                MajorScaleCount = 4,
                MinorScaleCount = 10,
                TopMajorTickMarkWidth = TopMajorTickMarkWidth,
                BottomMajorTickMarkWidth = BottomMajorTickMarkWidth,
                MinorTickMarkWidth = 19,
                MinorTickMarkOffsetX = 3,
                Min = 0,
                Max = MaxBc
            };
            AddChild(_verticalTickMarks);
            _titleFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, FontSizes.Title,
                    fontWeight: FontWeight.SemiBold);
            _baker = new Baker(context);
            RegisterResource(_baker);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;


        public static ID2D1PathGeometry CreateBcTrapezoidGeometry(ID2D1Factory factory, float ratio = 1)
        {
            var geometry = factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(new Vector2(0, (1 - ratio) * Height), FigureBegin.Filled);
                sink.AddLine(new Vector2(MathHelper.Lerp(ShortWidth, LongWidth, ratio), (1 - ratio) * Height));
                sink.AddLine(new Vector2(ShortWidth, Height));
                sink.AddLine(new Vector2(0, Height));
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            return geometry;
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(BakeBounds, Draw);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(BakeBounds, Draw);
        }

        private void Draw()
        {
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop,
                () =>
                {
                    Context.CommonBrush.Color = MonitorColors.White;
                    Context.DeviceContext.DrawDynamicText(Context.DwFactory, "BC", LongWidth / 2f,
                        VerticalTickMarks.TitleOffsetBottom, _titleFormat, Context.CommonBrush, 0.5f, 1);
                });
            Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.RecessedInner,
                () =>
                {
                    Context.CommonBrush.Color = MonitorColors.Recessed;
                    Context.DeviceContext.FillGeometry(_geometry, Context.CommonBrush);
                    Context.CommonBrush.Color = MonitorColors.RecessedDivider;
                    Context.DeviceContext.WithLayer(_geometry,
                        _ => { DrawBcHorizontalDividers(Context.DeviceContext, Context.CommonBrush); });
                });
        }

        public static void DrawBcHorizontalDividers(ID2D1DeviceContext dc, ID2D1SolidColorBrush brush)
        {
            const float spacing = Height / (DividerCount + 1);
            for (var i = 1; i <= DividerCount; i++)
            {
                var y = Height - spacing * i;
                dc.DrawLine(new Vector2(0, y), new Vector2(LongWidth - 1, y), brush, DividerStrokeWidth);
            }
        }
    }
}