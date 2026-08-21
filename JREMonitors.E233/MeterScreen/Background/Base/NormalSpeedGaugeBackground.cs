using System;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class NormalSpeedGaugeBackground : GaugeBackground
    {
        public const float SectorRadius = 162;
        public const float TickMarkSpacing = 6;
        public const float DividerRadius = 83;
        public const float DividerStrokeWidth = 3.5f;
        private readonly GaugeTickMarks _tickMarks;
        private readonly IDWriteTextFormat _tickTextFormat;

        public NormalSpeedGaugeBackground(RenderContext context) : base(context, 755, 563, 1,
            new GaugeStyle
            {
                Radius = SectorRadius,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 240
            })
        {
            _tickMarks = new GaugeTickMarks(context, 0, 0)
            {
                Radius = SectorRadius + TickMarkSpacing,
                StartAngleInDegrees = -210,
                SweepAngleInDegrees = 240,
                MajorScaleCount = 8,
                MinorScaleCount = 10,
                MajorTickMarkWidth = 25,
                MinorTickMarkWidth = 10,
                MinorTickMarkBoldedWidth = 15,
                MajorTickMarkStrokeWidth = 4,
                MinorTickMarkStrokeWidth = 3,
                MinorTickMarkBoldStrokeWidth = 4,
                MinorTickMarkOffset = 6,
                MajorTickMarkAction = OnDrawTickText,
                MinorTickMarkBoldInterval = 5,
                BoundsInflate = new RawRectF(-45, -30, 55, 30)
            };
            AddChild(_tickMarks);
            _tickTextFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, 40, fontWeight: FontWeight.SemiLight);
        }


        private void OnDrawTickText(GaugeTickMarks.GaugeTickMarkState state)
        {
            var vStep = 4 - Math.Abs(4 - state.Index);
            float h, v;
            switch (vStep)
            {
                case 0:
                    v = 0;
                    break;
                case 1:
                    v = 0.5f;
                    break;
                case 3:
                    v = 1.1f;
                    break;
                default:
                    v = 1;
                    break;
            }

            if (state.Index < 3)
                h = 1;
            else if (state.Index == 5)
                h = 0.3f;
            else if (state.Index < 6)
                h = 0.5f;
            else
                h = 0;

            var num = state.Index * 20;
            var text = num.ToString();

            var point = state.Outer.ForwardByAngle(state.Angle, 4);
            Context.CommonBrush.Color = MonitorColors.White;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, text, point.X, point.Y, _tickTextFormat,
                Context.CommonBrush, h, v, Fonts.ItalicDegrees, GetTickTextSpacings(num));
        }

        public static TextSpacing[] GetTickTextSpacings(int num)
        {
            var text = num.ToString();
            TextSpacing[] spacings;
            var globalSpacing = new TextSpacing
            {
                Index = 0,
                Length = text.Length,
                TrailingSpacing = 0
            };
            if (num >= 100)
                spacings = new[]
                {
                    globalSpacing,
                    new TextSpacing
                    {
                        Index = 0,
                        Length = 1,
                        TrailingSpacing = -6
                    }
                };
            else
                spacings = new[]
                {
                    globalSpacing
                };
            return spacings;
        }

        protected override void OnDrawInner()
        {
            Context.DeviceContext.WithLayer(Geometry, _ =>
            {
                var pos = Vector2.Zero;
                Context.CommonBrush.Color = MonitorColors.RecessedDivider;
                Context.DeviceContext.DrawEllipse(new Ellipse(pos, DividerRadius, DividerRadius),
                    Context.CommonBrush, DividerStrokeWidth);
                for (var i = 1; i < 8; i++)
                {
                    var angle = -210 + 30 * i;
                    var from = pos.ForwardByAngle(angle, DividerRadius);
                    var to = pos.ForwardByAngle(angle, SectorRadius + 5);
                    Context.DeviceContext.DrawLine(from, to, Context.CommonBrush, DividerStrokeWidth);
                }
            });
        }
    }
}