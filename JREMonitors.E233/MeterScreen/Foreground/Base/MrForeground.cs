using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.Needles;
using JREMonitors.E233.TickMarks;
using Vortice;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class MrForeground : MrBackground
    {
        private const float DividerStrokeWidth = BcBackground.DividerStrokeWidth;
        private const int MinEllipticalMr = 700;
        private const int MinNormalMr = 780;
        private const int MaxNormalMr = 880;
        private const float NeedleHeight = 17;
        private static readonly Color4 DividerForegroundColor = "#9C655D".ToColor4();
        private static readonly Color4 RedAreaColor = "#B84D3B".ToColor4();
        private static readonly Color4 PointColor = Colors.Red;
        private readonly PropertySlot<float> _clampedMr;
        private readonly VerticalNeedle _needle;
        private readonly PropertySlot<float> _redAreaBottom;
        private readonly PropertySlot<float> _redAreaTop;
        private readonly PropertySlot<bool> _truncateRedArea;
        private readonly VerticalTickMarks _verticalTickMarks;

        public MrForeground(RenderContext context, float x, float y) : base(context, x, y)
        {
            ShouldDrawTitle = false;
            _verticalTickMarks = new VerticalTickMarks(context, VerticalTickMarksOffsetX, 0, false)
            {
                NumAlwaysCenterAlign = true,
                TopMajorTickMarkWidth = 25,
                BottomMajorTickMarkWidth = 25,
                MajorTickMarkStrokeWidth = 3.5f,
                MinorTickMarkWidth = 11,
                Max = 1000
            };
            AddChild(_verticalTickMarks);
            _needle = new VerticalNeedle(context, 0, 0, NeedleHeight, PointColor, true, 140);
            _needle.RectPartWidth.Value = Width;
            AddChild(_needle);
            Mr = CreateRelayPropertySlot<float>();
            _clampedMr = CreateRelayPropertySlot(source: CreateComputed(() => MathHelper.Clamp(Mr, 0, 1000)));
            _truncateRedArea =
                CreatePropertySlot(DirtyType.Visual, source: CreateComputed(() => _clampedMr >= MinEllipticalMr));
            _redAreaTop = CreatePropertySlot(DirtyType.Visual,
                source: CreateComputed(() => _truncateRedArea
                    ? (300f - (MaxNormalMr - 700)) / 300 * Height
                    : (1000 - _clampedMr) / 1000 * Height));
            _redAreaBottom = CreatePropertySlot(DirtyType.Visual,
                source: CreateComputed(() => _truncateRedArea ? (300f - (MinNormalMr - 700)) / 300 * Height : Height));
            WatchEffect(EffectPhase.State, () =>
            {
                var truncate = _truncateRedArea.Value;
                var value = _clampedMr.Value;
                if (truncate)
                {
                    _verticalTickMarks.MajorScaleCount = 3;
                    _verticalTickMarks.MinorScaleCount = 5;
                    _verticalTickMarks.Min = 700;
                    _needle.Y.Value = (300 - (value - 700)) / 300 * Height;
                }
                else
                {
                    _verticalTickMarks.MajorScaleCount = 5;
                    _verticalTickMarks.MinorScaleCount = 4;
                    _verticalTickMarks.Min = 0;
                    _needle.Y.Value = (1000 - value) / 1000 * Height;
                }
            }, false);
            WatchEffect(() => MainBaker.Refresh(), _truncateRedArea, _redAreaTop, _redAreaBottom);
        }

        public PropertySlot<float> Mr { get; }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var nh = (float)Math.Ceiling(NeedleHeight);
                return new RectangleF(new PointF(-3, MathHelper.Min(-nh - 5, VerticalTickMarks.TitleOffsetTop)),
                    new SizeF(
                        _needle.FullWidth +
                        Shadows.GaugeNeedleDrop.MaxSpread,
                        Height + nh * 2 + Shadows.GaugeNeedleDrop.MaxSpread -
                        VerticalTickMarks.TitleOffsetBottom
                    ));
            }
        }

        private float ScaleCount => _truncateRedArea.Value ? 3 : 5;

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Slow : (float?)null;

        protected override void OnCustomDraw()
        {
            DrawBackgroundHorizontalDividers();
            DrawForegroundGeometries();
        }

        private void DrawBackgroundHorizontalDividers()
        {
            Context.CommonBrush.Color = MonitorColors.RecessedDivider;
            var spacing = Height / ScaleCount;
            for (var i = 1; i < ScaleCount; i++)
            {
                var y = spacing * i;
                Context.DeviceContext.DrawLine(
                    new Vector2(0, y),
                    new Vector2(Width, y),
                    Context.CommonBrush,
                    DividerStrokeWidth);
            }
        }

        private void DrawForegroundGeometries()
        {
            Context.CommonBrush.Color = DividerForegroundColor;
            var top = _redAreaTop.Value;
            var bottom = _redAreaBottom.Value;
            Context.CommonBrush.Color = RedAreaColor;
            Context.DeviceContext.FillRectangle(new RawRectF(0, top, Width, bottom), Context.CommonBrush);
            var spacing = Height / ScaleCount;
            for (var i = 1; i < ScaleCount; i++)
            {
                var y = spacing * i;
                if (y < top || y > bottom) continue;
                Context.DeviceContext.DrawLine(
                    new Vector2(0, y),
                    new Vector2(Width, y),
                    Context.CommonBrush,
                    DividerStrokeWidth
                );
            }
        }
    }
}