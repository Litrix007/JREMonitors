using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TickMarks;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class AtcSpeedGaugeBackground : GaugeBackground
    {
        public const float TickMarkInnerRadius = NormalSpeedGaugeBackground.SectorRadius + 2;
        public const float TickMarkMaxWidth = 24;
        private readonly GaugeTickMarks _tickMarks;
        private readonly IDWriteTextFormat _tickTextFormat;

        public AtcSpeedGaugeBackground(RenderContext context, float x, float y) : base(context, x, y, 1,
            new GaugeStyle
            {
                Radius = NormalSpeedGaugeBackground.SectorRadius,
                StartAngleInDegrees = -216,
                SweepAngleInDegrees = 252
            })
        {
            _tickMarks = new GaugeTickMarks(context, 0, 0)
            {
                Radius = TickMarkInnerRadius,
                StartAngleInDegrees = -216,
                SweepAngleInDegrees = 252,
                MajorScaleCount = 14,
                MinorScaleCount = 10,
                MinorBoldTickMarkColor = MonitorColors.MinorTickMark,
                MajorTickMarkWidth = TickMarkMaxWidth,
                MinorTickMarkWidth = 12,
                MinorTickMarkBoldedWidth = 24,
                MajorTickMarkStrokeWidth = 3,
                MinorTickMarkBoldStrokeWidth = 2,
                MinorTickMarkStrokeWidth = 2,
                MinorTickMarkBoldInterval = 5,
                MajorTickMarkAction = OnDrawTickText
            };
            AddChild(_tickMarks);
            _tickTextFormat =
                context.FontManager.GetOrCreateFormat(Fonts.CenturyGothic, 30, fontWeight: FontWeight.SemiLight);
        }

        private void OnDrawTickText(GaugeTickMarks.GaugeTickMarkState state)
        {
            Context.CommonBrush.Color = MonitorColors.White;
            if (state.Index % 2 == 1) return;
            var i = state.Index / 2;
            float h = 0, v = 0;
            switch (i)
            {
                case 0:
                    h = -0.2f;
                    v = 0.9f;
                    break;
                case 1:
                    h = -0.1f;
                    v = 0.6f;
                    break;
                case 2:
                    v = 0.2f;
                    break;
                case 3:
                    h = 0.4f;
                    break;
                case 4:
                    h = 0.6f;
                    break;
                case 5:
                    h = 1f;
                    v = 0.2f;
                    break;
                case 6:
                    h = 0.9f;
                    v = 0.6f;
                    break;
                case 7:
                    h = 0.9f;
                    v = 0.9f;
                    break;
            }

            var num = state.Index * 10;
            var text = num.ToString();
            var point = state.Outer.ForwardByAngle(state.Angle + 180, 30);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory,
                text, point.X, point.Y, _tickTextFormat,
                Context.CommonBrush, h, v,
                Fonts.ItalicDegrees, NormalSpeedGaugeBackground.GetTickTextSpacings(num));
        }

        protected override void OnDrawInner()
        {
            var pos = Vector2.Zero;
            Context.CommonBrush.Color = MonitorColors.RecessedDivider;
            Context.DeviceContext.WithLayer(Geometry, _ =>
            {
                Context.DeviceContext.DrawEllipse(
                    new Ellipse(pos, NormalSpeedGaugeBackground.DividerRadius,
                        NormalSpeedGaugeBackground.DividerRadius),
                    Context.CommonBrush, NormalSpeedGaugeBackground.DividerStrokeWidth);
                for (var i = 2; i < 14; i += 2)
                {
                    var angle = -216 + 18 * i;
                    var from = pos.ForwardByAngle(angle, NormalSpeedGaugeBackground.DividerRadius);
                    var to = pos.ForwardByAngle(angle, NormalSpeedGaugeBackground.SectorRadius + 5);
                    Context.DeviceContext.DrawLine(from, to, Context.CommonBrush,
                        NormalSpeedGaugeBackground.DividerStrokeWidth);
                }
            });
        }
    }
}