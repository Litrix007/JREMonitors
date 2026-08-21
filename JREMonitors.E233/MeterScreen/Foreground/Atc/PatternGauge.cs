using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Atc
{
    public class PatternGauge : Widget
    {
        public const float StrokeWidth = 14;

        public const float Radius = AtcSpeedGaugeBackground.TickMarkInnerRadius +
                                    AtcSpeedGaugeBackground.TickMarkMaxWidth +
                                    StrokeWidth * 1.2f;

        private static readonly Color4 Color = ColorHelper.FromHex("#0E1619");
        private readonly AtcNeedle _atcNeedle;
        private readonly Baker _baker;
        private readonly ID2D1SolidColorBrush _brush;
        private readonly ID2D1PathGeometry _geometry;
        private readonly ID2D1StrokeStyle1 _strokeStyle;


        public PatternGauge(RenderContext context, float x, float y) : base(context, x, y)
        {
            _geometry = context.D2D1Factory.CreatePathGeometry();
            using (var sink = _geometry.Open())
            {
                sink.AddArcLineFigure(Radius, -216, 216);
                sink.Close();
            }

            AddResource(_geometry);
            _strokeStyle = Context.D2D1Factory.CreateStrokeStyle(new StrokeStyleProperties1
            {
                StartCap = CapStyle.Flat,
                EndCap = CapStyle.Flat,
                LineJoin = LineJoin.Miter
            });
            SelfRelativeDirtyBounds =
                GeometryHelper.GetArcBounds(Radius - StrokeWidth / 2, Radius + StrokeWidth / 2, -216, 216);
            _brush = Context.DeviceContext.CreateSolidColorBrush(Color);
            AddResource(_brush);
            _baker = new Baker(context.DeviceContext);
            AddResource(_baker);
            _atcNeedle = new AtcNeedle(context, 0, 0,
                Radius - StrokeWidth);
            _atcNeedle.On = true;
            AddChild(_atcNeedle);
        }

        public override RectangleF SelfRelativeDirtyBounds { get; }
        public override float? RefreshSpeed => RefreshSpeeds.Fast;

        public void SetSpeedLimit(float limit)
        {
            _atcNeedle.Degree = -216 + 252f * limit / 140;
            _atcNeedle.OnColor = limit > 0 ? MonitorColors.NormalGreen : MonitorColors.Red;
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(Vector2.Zero, SelfRelativeDirtyBounds,
                () =>
                {
                    Context.ShadowProcessor.DrawWithInnerShadows(Shadows.RecessedHalfAlpha,
                        dc => { dc.DrawGeometry(_geometry, _brush, StrokeWidth, _strokeStyle); });
                });
        }
    }
}