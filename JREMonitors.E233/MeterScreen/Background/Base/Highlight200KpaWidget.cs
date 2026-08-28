using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class Highlight200KpaWidget : Widget
    {
        private const float StickWidth = 59;
        private const float StickHeight = 5;
        private const float UpWidth1 = 15;
        private const float UpWidth2 = 50;
        private const float UpHeight = 17;
        private const float DownWidth1 = 7;
        private const float DownWidth2 = 51;
        private const float DownHeight = 7;
        private const float VerticalAlignment200 = 0.9f - 0.55f * (0.9f - 3f / 4);

        private const float BorderSlope = (BcBackground.BottomMajorTickMarkWidth - BcBackground.TopMajorTickMarkWidth) /
                                          BcBackground.Height;

        private readonly Baker _baker;
        private readonly IDWriteTextFormat _format;
        private readonly ID2D1PathGeometry _geometry;

        public Highlight200KpaWidget(RenderContext context, float x, float y) : base(context, x, y)
        {
            _format = VerticalTickMarks.GetOrCreateTickTextFormat(context, true);

            _geometry = CreateHighlight200KpaGeometry();
            RegisterResource(_geometry);
            _baker = new Baker(context);
            RegisterResource(_baker);
        }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var rect = new RectangleF(-5, -UpHeight - StickHeight, StickWidth + UpWidth1 + UpWidth2 + 10,
                    UpHeight + StickHeight * 2 + DownHeight);
                rect.Inflate(5, 10);
                return rect;
            }
        }

        private ID2D1PathGeometry CreateHighlight200KpaGeometry()
        {
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.SetFillMode(FillMode.Winding);
                const float startX = StickHeight * BorderSlope;
                const float startY = -StickHeight;
                const float endX = -StickHeight * BorderSlope;
                const float endY = StickHeight;
                var isTopOrLeft = new[]
                {
                    false, true, false, false, false, true, true, false
                };
                var radius = new[]
                {
                    0f, 4f, 4f, 4f, 4f, 4f, 4f, 0f
                };
                var vertices = new[]
                {
                    new Vector2(startX, startY),
                    new Vector2(startX + StickWidth, startY),
                    new Vector2(startX + StickWidth + UpWidth1, startY - UpHeight),
                    new Vector2(startX + StickWidth + UpWidth1 + UpWidth2, startY - UpHeight),
                    new Vector2(startX + StickWidth + DownWidth1 + DownWidth2, endY + DownHeight),
                    new Vector2(startX + StickWidth + DownWidth1, endY + DownHeight),
                    new Vector2(startX + StickWidth, endY),
                    new Vector2(endX, endY)
                };

                sink.AddRoundedPolygonFigure(vertices, radius, isTopOrLeft);
                sink.Close();
            }

            return geometry;
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds,
                () =>
                {
                    Context.CommonBrush.Color = Colors.Red;
                    Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.NormalNeedleInner,
                        () => { Context.DeviceContext.FillGeometry(_geometry, Context.CommonBrush); });
                    var textLocalX = BcBackground.LongWidth + 29 + VerticalTickMarks.TextSpacingX - X.Value;
                    Context.CommonBrush.Color = Colors.Black;
                    Context.DeviceContext.DrawDynamicText(
                        Context.DwFactory,
                        "200",
                        textLocalX,
                        -1,
                        _format,
                        Context.CommonBrush,
                        0f,
                        VerticalAlignment200,
                        Fonts.ItalicDegrees
                    );
                });
        }
    }
}