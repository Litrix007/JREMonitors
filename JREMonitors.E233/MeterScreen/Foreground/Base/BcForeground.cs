using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.Needles;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class BcForeground : Widget
    {
        private const float NeedleHeight = 17;
        private const float NeedleExtraWidth = 10;
        private const float ScaleMajor = BcBackground.Height / 4;
        public static readonly Color4 BcAreaColor = "#8E8F99".ToColor4();
        public static readonly Color4 DividerForegroundColor = "#70707B".ToColor4();
        private readonly PropertySlot<float> _clampedBc;
        private readonly Highlight200KpaWidget _highlight200KpaWidget;
        private readonly VerticalNeedle _needle;
        private readonly Baker _trapezoidBaker;
        private readonly ID2D1PathGeometry _trapezoidGeometry;

        public BcForeground(RenderContext context, float x, float y) : base(context, x, y)
        {
            _trapezoidBaker = new Baker(context);
            RegisterResource(_trapezoidBaker);
            _highlight200KpaWidget = new Highlight200KpaWidget(context, MathHelper.Lerp(
                    BcBackground.TopMajorTickMarkWidth,
                    BcBackground.BottomMajorTickMarkWidth,
                    3 * ScaleMajor / BcBackground.Height) - 1,
                3 * ScaleMajor);
            _needle = new VerticalNeedle(context, 0, 0, NeedleHeight, MonitorColors.White);
            _trapezoidGeometry = BcBackground.CreateBcTrapezoidGeometry(Context.D2D1Factory);
            RegisterResource(_trapezoidGeometry);
            Bc = CreateRelayPropertySlot<float>();
            _clampedBc =
                CreatePropertySlot(DirtyType.Visual, source: CreateComputed(() => MathHelper.Clamp(Bc, 0, 800)));
            Highlight200Kpa = CreatePropertySlot<bool>(DirtyType.Visual);
            _highlight200KpaWidget.IsVisible.Bind(CreateComputed(() => Highlight200Kpa.Value));
            AddChild(_highlight200KpaWidget);
            _needle.Y.Bind(CreateComputed(() => BcBackground.Height * (1 - _clampedBc / BcBackground.MaxBc)));
            _needle.RectPartWidth.Bind(CreateComputed(() =>
                NeedleExtraWidth + MathHelper.Lerp(
                    BcBackground.ShortWidth,
                    BcBackground.LongWidth,
                    _clampedBc / BcBackground.MaxBc
                )));
            AddChild(_needle);
        }

        public PropertySlot<float> Bc { get; }
        public PropertySlot<bool> Highlight200Kpa { get; }

        public override float? RefreshSpeed =>
            IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        private static RectangleF TrapezoidBounds => new RectangleF(0, 0, BcBackground.LongWidth, BcBackground.Height);

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var height = BcBackground.Height * _clampedBc / BcBackground.MaxBc;
                return new RectangleF(0, BcBackground.Height - height, BcBackground.LongWidth, height);
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _trapezoidBaker.Alloc(new RectangleF(0, 0, BcBackground.LongWidth, BcBackground.Height));
            _trapezoidBaker.BakeAndDraw(TrapezoidBounds, Draw, sourceBounds: SelfRelativeDirtyBounds);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _trapezoidBaker.BakeAndDraw(TrapezoidBounds, Draw, sourceBounds: SelfRelativeDirtyBounds);
        }

        private void Draw()
        {
            Context.DeviceContext.WithLayer(_trapezoidGeometry, _ =>
            {
                Context.CommonBrush.Color = BcAreaColor;
                Context.DeviceContext.FillGeometry(_trapezoidGeometry, Context.CommonBrush);
                Context.CommonBrush.Color = DividerForegroundColor;
                BcBackground.DrawBcHorizontalDividers(Context.DeviceContext, Context.CommonBrush);
            });
        }
    }
}