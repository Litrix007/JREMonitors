using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class GaugeBackground : Widget
    {
        private readonly Baker _baker;
        private readonly GaugeStyle _style;
        protected readonly ID2D1PathGeometry Geometry;

        public GaugeBackground(RenderContext context, float x, float y, float scale, GaugeStyle style) : base(context,
            x, y, scale)
        {
            _style = style;
            Geometry = CreateSectorGeometry(context.D2D1Factory, style.Radius, style.StartAngleInDegrees,
                style.SweepAngleInDegrees);
            RegisterResource(Geometry);
            _baker = new Baker(context);
            RegisterResource(_baker);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected virtual RectangleF BakeBounds =>
            GeometryHelper.GetArcBounds(0, _style.Radius + 2, _style.StartAngleInDegrees,
                _style.SweepAngleInDegrees).SnapToPixels();

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(BakeBounds, () => SelfDraw(totalScale));
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(BakeBounds, () => SelfDraw(totalScale));
        }

        protected virtual void SelfDraw(float totalScale)
        {
            Context.InnerShadowProcessor.DrawWithInnerShadows(
                Shadows.GaugeRecessedInner.Select(shadow => shadow / totalScale).ToArray(),
                () =>
                {
                    Context.CommonBrush.Color = MonitorColors.Recessed;
                    Context.DeviceContext.FillGeometry(Geometry, Context.CommonBrush);
                    OnDrawInner();
                });
        }

        protected virtual void OnDrawInner()
        {
        }

        private static ID2D1PathGeometry CreateSectorGeometry(ID2D1Factory factory, float radius,
            float startAngleInDegrees, float sweepAngleInDegrees)
        {
            var geometry = factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(Vector2.Zero, FigureBegin.Filled);
                var startRad = startAngleInDegrees * (MathHelper.Pi / 180f);
                var endRad = (startAngleInDegrees + sweepAngleInDegrees) * (MathHelper.Pi / 180f);
                var startPoint = new Vector2(radius * MathHelper.Cos(startRad), radius * MathHelper.Sin(startRad));
                var endPoint = new Vector2(radius * MathHelper.Cos(endRad), radius * MathHelper.Sin(endRad));
                sink.AddLine(startPoint);
                var arc = new ArcSegment
                {
                    Point = endPoint,
                    Size = new SizeF(radius, radius),
                    RotationAngle = 0,
                    SweepDirection = SweepDirection.Clockwise,
                    ArcSize = Math.Abs(sweepAngleInDegrees) > 180 ? ArcSize.Large : ArcSize.Small
                };
                sink.AddArc(arc);
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            return geometry;
        }
    }
}