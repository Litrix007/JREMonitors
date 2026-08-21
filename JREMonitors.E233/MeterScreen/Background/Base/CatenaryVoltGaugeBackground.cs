using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class CatenaryVoltGaugeBackground : GaugeBackground
    {
        public const float SectorRadius = DeviceVoltGaugeBackground.SectorRadius;
        public const float DividerStrokeWidth = DeviceVoltGaugeBackground.DividerStrokeWidth;

        private static readonly TextSpacing SpecialSpacing = new TextSpacing
        {
            Index = 0,
            Length = 1,
            TrailingSpacing = -4
        };

        private readonly PropertySlot<bool> _boldCenterMinorTicks;

        private readonly GaugeTickMarks _tickMarks;
        private readonly IDWriteTextFormat _tickTextFormat;
        private readonly IDWriteTextFormat _titleFormat;

        public CatenaryVoltGaugeBackground(RenderContext context, float x, float y,
            float scale = 1) : base(context, x,
            y, scale, new GaugeStyle
            {
                Radius = SectorRadius,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 210
            })
        {
            _tickMarks = new GaugeTickMarks(context, 0, 0)
            {
                Radius = SectorRadius + 5,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 210,
                MajorScaleCount = 4,
                MinorScaleCount = 10,
                MajorTickMarkWidth = 13,
                MinorTickMarkWidth = 11,
                MinorTickMarkBoldedWidth = 11,
                MajorTickMarkStrokeWidth = 4,
                MinorTickMarkStrokeWidth = 3,
                MinorTickMarkBoldStrokeWidth = 3,
                MinorTickMarkBoldInterval = 2,
                MajorTickMarkAction = OnDrawTickText,
                BoundsInflate = new RawRectF(-5, -10, 10, 10)
            };
            AddChild(_tickMarks);
            _tickTextFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, DeviceVoltGaugeBackground.TickTextSize);
            _titleFormat =
                context.FontManager.GetOrCreateFormat(Fonts.ArialFamily, DeviceVoltGaugeBackground.TitleSize,
                    fontWeight: FontWeight.Bold);
            _boldCenterMinorTicks = CreatePropertySlot<bool>(DirtyType.Visual);
            WatchEffect(EffectPhase.State, () =>
            {
                _tickMarks.MinorBoldTickMarkColor =
                    _boldCenterMinorTicks.Value ? MonitorColors.White : MonitorColors.MinorTickMark;
            }, false);
        }

        protected override RectangleF BakeBounds
        {
            get
            {
                var bounds = base.BakeBounds;
                bounds.Width += 25;
                bounds.Height += 25;
                return bounds;
            }
        }

        public bool BoldCenterMinorTicks
        {
            get => _boldCenterMinorTicks;
            set => _boldCenterMinorTicks.Value = value;
        }

        private void OnDrawTickText(GaugeTickMarks.GaugeTickMarkState state)
        {
            float h = 0, v;
            string text;
            TextSpacing specialSpacing = default;
            switch (state.Index)
            {
                case 0:
                    h = 1;
                    v = 0.5f;
                    text = "0";
                    break;
                case 1:
                    h = 1;
                    v = 0.8f;
                    text = ".5";
                    break;
                case 2:
                    h = 1;
                    v = 1;
                    text = "1";
                    specialSpacing = SpecialSpacing;
                    break;
                case 3:
                    v = 1;
                    text = "1.5";
                    specialSpacing = SpecialSpacing;
                    break;
                default:
                    v = 0.7f;
                    text = "2";
                    break;
            }

            var globalSpacing = new TextSpacing
            {
                Index = 0,
                Length = text.Length,
                TrailingSpacing = -1
            };
            var point = state.Outer.ForwardByAngle(state.Angle, 5);
            Context.CommonBrush.Color = MonitorColors.White;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory,
                text, point.X, point.Y, _tickTextFormat,
                Context.CommonBrush, h, v,
                Fonts.ItalicDegrees, new[]
                {
                    globalSpacing, specialSpacing
                });
        }

        protected override void SelfDraw(float totalScale)
        {
            base.SelfDraw(totalScale);
            Context.CommonBrush.Color = MonitorColors.MeterTitleGrey;
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop,
                () =>
                {
                    Context.DeviceContext.DrawDynamicText(Context.DwFactory, "kV",
                        DeviceVoltGaugeBackground.TitleOffsetX,
                        DeviceVoltGaugeBackground.TitleOffsetY, _titleFormat, Context.CommonBrush, 1,
                        weights: new[] { new TextWeight(0, 1, FontWeight.Medium) });
                });
        }

        protected override void OnDrawInner()
        {
            base.OnDrawInner();
            Context.CommonBrush.Color = MonitorColors.RecessedDivider;
            Context.DeviceContext.WithLayer(Geometry, _ =>
            {
                var to = new Vector2(0, -SectorRadius - 5);
                Context.DeviceContext.DrawLine(Vector2.Zero, to, Context.CommonBrush, DividerStrokeWidth);
            });
        }
    }
}