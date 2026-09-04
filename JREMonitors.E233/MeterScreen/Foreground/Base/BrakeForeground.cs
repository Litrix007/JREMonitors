using System.Drawing;
using System.Linq;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class BrakeForeground : Widget
    {
        public const float TextSize = BrakeBackground.PieceHeight + 8;
        public static readonly Color4 BrakeNormalColor = "#FFF14E".ToColor4();
        public static readonly Color4 BrakeMaxColor = "#FFC96E".ToColor4();
        private readonly Baker[] _bakers = new Baker[BrakeBackground.MaxBrake];
        private readonly PropertySlot<int> _clampedBrake;
        private readonly ID2D1PathGeometry _maxGeometry;
        private readonly ID2D1PathGeometry[] _normalGeometries = new ID2D1PathGeometry[BrakeBackground.MaxBrake];
        private readonly IDWriteTextFormat _textFormat;

        public BrakeForeground(RenderContext context, float x, float y) : base(context, x, y)
        {
            for (var i = 0; i < BrakeBackground.MaxBrake; i++)
            {
                _normalGeometries[i] =
                    BrakeBackground.CreateBrakeGeometry(context.D2D1Factory, i < BrakeBackground.MaxBrake - 1,
                        MathHelper.Min(i + 1, BrakeBackground.MaxBrake - 1));
                RegisterResource(_normalGeometries[i]);
                _bakers[i] = new Baker(context);
                RegisterResource(_bakers[i]);
            }

            _maxGeometry = BrakeBackground.CreateBrakeGeometry(context.D2D1Factory, true, BrakeBackground.MaxBrake,
                BrakeBackground.MaxBrake - 1);
            RegisterResource(_maxGeometry);
            _textFormat = context.FontManager.GetOrCreateFormat(Fonts.ArialFamily, TextSize);
            Brake = CreateRelayPropertySlot<int>();
            _clampedBrake = CreatePropertySlot(DirtyType.Visual, source: CreateComputed(() =>
            {
                if (Brake <= 0 || Brake > BrakeBackground.MaxBrake) return 0;
                return Brake;
            }));
        }

        public PropertySlot<int> Brake { get; }


        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                const float circleExtra = BrakeBackground.PieceHeight * (BrakeBackground.CircleRatio - 1) / 2;
                var maxSpread = Shadows.RecessedInner.Max(x => x.MaxSpread);
                return new RectangleF(
                    new PointF(-BrakeBackground.LongWidth, -BrakeBackground.Height - circleExtra),
                    new SizeF(BrakeBackground.LongWidth + maxSpread,
                        BrakeBackground.Height + circleExtra * 2 + maxSpread));
            }
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        protected override void OnStaticWarmUp(float totalScale)
        {
            for (var i = 1; i <= BrakeBackground.MaxBrake; i++)
            {
                var j = i;
                _bakers[i - 1].BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(j),
                    overrideInterpolationMode: InterpolationMode.Cubic);
            }
        }

        private void Draw(int brake)
        {
            Context.DropShadowProcessor.DrawWithDropShadows(new[] { Shadows.GaugeNeedleDrop },
                () =>
                {
                    Context.CommonBrush.Color = BrakeNormalColor;
                    Context.DeviceContext.FillGeometry(_normalGeometries[brake - 1], Context.CommonBrush);
                    if (brake < BrakeBackground.MaxBrake) return;
                    Context.CommonBrush.Color = BrakeMaxColor;
                    Context.DeviceContext.FillGeometry(_maxGeometry, Context.CommonBrush);
                });
            var bottomY = (1 - brake) * BrakeBackground.FullySpacingY;
            var pieceShortWidth =
                MathHelper.Lerp(BrakeBackground.ShortWidth, BrakeBackground.LongWidth,
                    -bottomY / BrakeBackground.Height);
            var pieceLongWidth = MathHelper.Lerp(BrakeBackground.ShortWidth, BrakeBackground.LongWidth,
                (-bottomY + BrakeBackground.PieceHeight) / BrakeBackground.Height);
            var centerX = -MathHelper.Lerp(pieceShortWidth, pieceLongWidth, 0.5f) / 2;
            var centerY = bottomY - BrakeBackground.PieceHeight / 2;
            Context.CommonBrush.Color = Colors.Black;
            Context.DeviceContext.DrawDynamicText(
                Context.DwFactory,
                brake.ToString(),
                centerX,
                centerY,
                _textFormat,
                Context.CommonBrush, 0.5f, 0.5f
            );
        }

        protected override void OnDraw(float totalScale)
        {
            if (_clampedBrake == 0) return;
            var i = _clampedBrake - 1;
            if (i > 7) i = 7;
            _bakers[i].BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(i),
                overrideInterpolationMode: InterpolationMode.Cubic);
        }
    }
}