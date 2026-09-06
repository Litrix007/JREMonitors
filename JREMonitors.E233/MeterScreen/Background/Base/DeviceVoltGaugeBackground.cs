using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class DeviceVoltGaugeBackground : GaugeBackground
    {
        public const float SectorRadius = 90;
        public const float TickMarkSpacing = 10;
        public const float DividerStrokeWidth = 3;
        public const float MajorTickMarkWidth = 13;
        public const float TickTextSize = 27;
        public const float TitleSize = 35;
        public const float TitleOffsetX = SectorRadius + TickMarkSpacing + MajorTickMarkWidth / 2;
        public const float TitleOffsetY = 30;
        private readonly GaugeTickMarks _tickMarks;
        private readonly IDWriteTextFormat _tickTextFormat;
        private readonly IDWriteTextFormat _titleFormat;

        public DeviceVoltGaugeBackground(RenderContext context, float x, float y, float scale = 1) : base(context, x, y,
            scale, new GaugeStyle
            {
                Radius = SectorRadius,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 210
            })
        {
            _tickMarks = new GaugeTickMarks(context, 0, 0)
            {
                Radius = SectorRadius + TickMarkSpacing,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 210,
                MajorScaleCount = 3,
                MinorScaleCount = 25,
                MajorTickMarkWidth = MajorTickMarkWidth,
                MinorTickMarkWidth = 9,
                MinorTickMarkBoldedWidth = 11,
                MajorTickMarkStrokeWidth = 3,
                MinorTickMarkStrokeWidth = 2,
                MinorTickMarkBoldStrokeWidth = 3,
                MinorTickMarkBoldInterval = MinorTickMarkBoldInterval,
                MinorTickMarkOffset = 2,
                MajorTickMarkAction = OnDrawTickText,
                BoundsInflate = new RawRectF(0, -10, 35, 10)
            };
            AddChild(_tickMarks);
            _tickTextFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, TickTextSize);
            _titleFormat =
                context.FontManager.GetOrCreateFormat(Fonts.ArialFamily, TitleSize, fontWeight: FontWeight.Bold);
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

        public int MinorTickMarkBoldInterval { get; set; } = 5;

        private void OnDrawTickText(GaugeTickMarks.GaugeTickMarkState state)
        {
            float h = 0, v;
            switch (state.Index)
            {
                case 0:
                    h = 1;
                    v = 0.5f;
                    break;
                case 1:
                    h = 1;
                    v = 1;
                    break;
                case 2:
                    h = 0.3f;
                    v = 1;
                    break;
                default:
                    v = 0.9f;
                    break;
            }

            TextSpacing[] spacings;
            var num = state.Index * 50;
            var text = num.ToString();
            var globalSpacing = new TextSpacing
            {
                Index = 0,
                Length = text.Length,
                TrailingSpacing = -1
            };
            if (num >= 100)
                spacings = new[]
                {
                    globalSpacing,
                    new TextSpacing
                    {
                        Index = 0,
                        Length = 1,
                        TrailingSpacing = -4
                    }
                };
            else
                spacings = new[]
                {
                    globalSpacing
                };

            var point = state.Outer.ForwardByAngle(state.Angle, 4);
            Context.CommonBrush.Color = MonitorColors.White;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory,
                text, point.X, point.Y, _tickTextFormat,
                Context.CommonBrush, h, v,
                Fonts.ItalicDegrees, spacings);
        }

        protected override void SelfDraw(float totalScale)
        {
            base.SelfDraw(totalScale);
            Context.CommonBrush.Color = MonitorColors.MeterTitleGrey;
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop,
                () =>
                {
                    Context.DeviceContext.DrawDynamicText(Context.DwFactory, "V", TitleOffsetX, TitleOffsetY,
                        _titleFormat,
                        Context.CommonBrush, 1);
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