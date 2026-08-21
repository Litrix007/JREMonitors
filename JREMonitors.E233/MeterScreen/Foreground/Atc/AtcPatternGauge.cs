using System.Drawing;
using System.Linq;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Atc
{
    public class AtcPatternGauge : Widget
    {
        private const float StrokeWidth = 14;

        private const float Radius = AtcSpeedGaugeBackground.TickMarkInnerRadius +
                                     AtcSpeedGaugeBackground.TickMarkMaxWidth +
                                     StrokeWidth * 1.2f;

        private static readonly OffsetInnerShadow[] InnerShadows =
            Shadows.RecessedInner.Select(s => s.With(color: s.Color.MultiplyAlpha(0.5f))).ToArray();

        private static readonly Color4 Color = "#0E1619".ToColor4();
        private readonly AtcNeedle _atcNeedle;
        private readonly Baker _baker;
        private readonly ID2D1PathGeometry _geometry;
        private readonly ID2D1StrokeStyle1 _strokeStyle;

        public AtcPatternGauge(RenderContext context, float x, float y) : base(context, x, y)
        {
            _geometry = context.D2D1Factory.CreatePathGeometry();
            using (var sink = _geometry.Open())
            {
                sink.AddArcLineFigure(Radius, -216, 216);
                sink.Close();
            }

            RegisterResource(_geometry);
            _strokeStyle = Context.D2D1Factory.CreateStrokeStyle(new StrokeStyleProperties1
            {
                StartCap = CapStyle.Flat,
                EndCap = CapStyle.Flat,
                LineJoin = LineJoin.Miter
            });
            _baker = new Baker(context);
            RegisterResource(_baker);
            _atcNeedle = new AtcNeedle(context, 0, 0, Radius - StrokeWidth, true);
            AddChild(_atcNeedle);
            TurnOff = CreateRelayPropertySlot<bool>();
            SpeedLimit = CreateRelayPropertySlot<int>();
            _atcNeedle.IsVisible.Bind(CreateComputed(() => !TurnOff));
            _atcNeedle.Degree.Bind(CreateComputed(() => -216 + 252f * SpeedLimit / 140));
            _atcNeedle.OnColor.Bind(
                CreateComputed(() => SpeedLimit > 0 ? MonitorColors.NormalGreen : MonitorColors.Red));
        }

        public PropertySlot<bool> TurnOff { get; }
        public PropertySlot<int> SpeedLimit { get; }

        public override RectangleF SelfRelativeDirtyBounds { get; } =
            GeometryHelper.GetArcBounds(Radius - StrokeWidth / 2, Radius + StrokeWidth / 2, -216, 216);

        public override float? RefreshSpeed => RefreshSpeeds.Slow;

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        private void Draw()
        {
            Context.InnerShadowProcessor.DrawWithInnerShadows(InnerShadows,
                () =>
                {
                    Context.CommonBrush.Color = Color;
                    Context.DeviceContext.DrawGeometry(_geometry, Context.CommonBrush, StrokeWidth, _strokeStyle);
                });
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }
    }
}