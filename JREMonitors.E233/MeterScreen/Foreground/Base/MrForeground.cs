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
        private const int MinEllipticalMr = 710;
        private const int MinNormalMr = 780;
        private const int MaxNormalMr = 880;
        private const float NeedleHeight = 17;
        private const float EllipticalRedAreaTop = (300f - (MaxNormalMr - 700f)) / 300f * Height;
        private const float EllipticalRedAreaBottom = (300f - (MinNormalMr - 700f)) / 300f * Height;
        private const float FixedRedAreaTop = (1000f - MaxNormalMr) / 1000f * Height;
        private const float FixedRedAreaBottom = (1000f - MinNormalMr) / 1000f * Height;
        private static readonly Color4 DividerForegroundColor = "#9C655D".ToColor4();
        private static readonly Color4 RedAreaColor1 = "#9D4638".ToColor4();
        private static readonly Color4 RedAreaColor2 = "#B84D3B".ToColor4();
        private static readonly Color4 PointColor = Colors.Red;
        private readonly PropertySlot<float> _dynamicRedAreaTop;
        private readonly VerticalNeedle _needle;
        private readonly PropertySlot<bool> _truncateRedArea;

        public MrForeground(RenderContext context, float x, float y) : base(context, x, y)
        {
            ShouldDrawTitle = false;
            var verticalTickMarks = new VerticalTickMarks(context, VerticalTickMarksOffsetX, 0, false)
            {
                NumAlwaysCenterAlign = true,
                TopMajorTickMarkWidth = 25,
                BottomMajorTickMarkWidth = 25,
                MajorTickMarkStrokeWidth = 3.5f,
                MinorTickMarkWidth = 11,
                MinorTickMarkBoldInterval = 0,
                Max = 1000
            };
            AddChild(verticalTickMarks);
            _needle = new VerticalNeedle(context, 0, 0, NeedleHeight, PointColor, true, 140);
            _needle.RectPartWidth.Value = Width;
            AddChild(_needle);
            Mr = CreateRelayPropertySlot<float>();
            var clampedMr = CreateComputed(() => MathHelper.Clamp(Mr, 0, 1000));
            _truncateRedArea = CreatePropertySlot(DirtyType.Visual,
                source: CreateComputed(() => clampedMr >= MinEllipticalMr));
            _dynamicRedAreaTop = CreatePropertySlot(DirtyType.Visual,
                source: CreateComputed(() => (1000f - clampedMr) / 1000f * Height));
            WatchEffect(EffectPhase.State, () =>
            {
                var truncate = _truncateRedArea.Value;
                var value = clampedMr.Value;
                if (truncate)
                {
                    verticalTickMarks.MajorScaleCount = 3;
                    verticalTickMarks.MinorScaleCount = 5;
                    verticalTickMarks.Min = 700;
                    _needle.Y.Value = (300 - (value - 700)) / 300 * Height;
                }
                else
                {
                    verticalTickMarks.MajorScaleCount = 5;
                    verticalTickMarks.MinorScaleCount = 2;
                    verticalTickMarks.Min = 0;
                    _needle.Y.Value = (1000 - value) / 1000 * Height;
                }

                verticalTickMarks.MajorTickMarkLengthMatchesMinor = !truncate;
            }, false);
            WatchEffect(EffectPhase.Commit, () =>
            {
                if (!_truncateRedArea) _dynamicRedAreaTop.Track();

                MainBaker.Refresh();
            });
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

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

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
            if (_truncateRedArea.Value)
            {
                FillRedArea(RedAreaColor1, EllipticalRedAreaTop, EllipticalRedAreaBottom);
            }
            else
            {
                FillRedArea(RedAreaColor1, FixedRedAreaTop, FixedRedAreaBottom);
                FillRedArea(RedAreaColor2, _dynamicRedAreaTop.Value, Height);
            }
        }

        private void FillRedArea(Color4 color, float top, float bottom)
        {
            Context.CommonBrush.Color = color;
            Context.DeviceContext.FillRectangle(new RawRectF(0, top, Width, bottom), Context.CommonBrush);
            var spacing = Height / ScaleCount;
            for (var i = 1; i < ScaleCount; i++)
            {
                var y = spacing * i;
                if (y < top || y > bottom) continue;
                Context.CommonBrush.Color = DividerForegroundColor;
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