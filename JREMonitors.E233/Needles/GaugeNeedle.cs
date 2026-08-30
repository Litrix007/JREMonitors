using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Needles
{
    public class GaugeNeedle : Widget
    {
        private const float InnerCircleRadiusRatio = 0.6f;
        private const float CapRadius = 1.7f;
        private readonly float _backRectWidth;
        private readonly Baker _baker;
        private readonly float _frontWidth;
        private readonly ResourceSlot<ID2D1PathGeometry> _geometry;
        private readonly float _height;
        private readonly OffsetInnerShadow[] _innerShadows;

        public GaugeNeedle(RenderContext context, float x, float y, float frontWidth,
            OffsetInnerShadow[] innerShadows = null, float heightRatio = 1 / 10f, float scale = 1,
            float trangleWidthRatio = 1, float backRectWidthRatio = 0.15f) : base(context, x, y, scale)
        {
            _frontWidth = frontWidth;
            _height = _frontWidth * heightRatio;
            _backRectWidth = _frontWidth * backRectWidthRatio;
            _innerShadows = innerShadows ?? Array.Empty<OffsetInnerShadow>();
            _baker = new Baker(context);
            RegisterResource(_baker);
            Degree = CreatePropertySlot<float>(DirtyType.Visual);
            AnchorColor = CreatePropertySlot(DirtyType.Visual, MonitorColors.MeterTitleGrey);
            NeedleColor = CreatePropertySlot(DirtyType.Visual, MonitorColors.White);
            TrangleWidthRatio = CreatePropertySlot(DirtyType.Visual, trangleWidthRatio);
            _geometry = WatchResource(() => BuildGeometry(TrangleWidthRatio));
            WatchEffect(
                () => { _baker.Refresh(); },
                AnchorColor,
                NeedleColor,
                Degree
            );
        }

        public PropertySlot<float> Degree { get; }
        public PropertySlot<Color4> AnchorColor { get; }
        public PropertySlot<Color4> NeedleColor { get; }
        public PropertySlot<float> TrangleWidthRatio { get; }

        public override RectangleF SelfRelativeDirtyBounds => GetAabbForDegree(Degree);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds.SnapToPixels(), () => Draw(totalScale));
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds.SnapToPixels(), () => Draw(totalScale));
        }

        private void Draw(float totalScale)
        {
            Context.CommonBrush.Color = NeedleColor;
            Context.DropShadowProcessor.DrawWithDropShadows(new[] { Shadows.GaugeNeedleDrop },
                shadowAction: DrawNeedle);
            Context.InnerShadowProcessor.DrawWithInnerShadows(
                _innerShadows.Select(shadow => shadow / totalScale).ToArray(), DrawNeedle);
            var radius = _height * InnerCircleRadiusRatio / 2;
            Context.CommonBrush.Color = AnchorColor;
            Context.DeviceContext.FillEllipse(new Ellipse(Vector2.Zero, radius, radius), Context.CommonBrush);
        }

        private void DrawNeedle()
        {
            var oldTransform = Context.DeviceContext.Transform;
            Context.DeviceContext.Transform = RenderHelper.CreateRotationMatrixInDegrees(Degree) * oldTransform;
            Context.CommonBrush.Color = NeedleColor;
            Context.DeviceContext.FillGeometry(_geometry.Value, Context.CommonBrush);
            Context.DeviceContext.Transform = oldTransform;
        }

        private ID2D1PathGeometry BuildGeometry(float triangleWidthRatio)
        {
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                var frontRectWidth = _frontWidth - _height * triangleWidthRatio;
                sink.BeginFigure(new Vector2(0 - _backRectWidth, 0 - _height / 2), FigureBegin.Filled);
                sink.AddLine(new Vector2(0 + frontRectWidth, 0 - _height / 2));
                sink.AddLine(new Vector2(0 + _frontWidth, 0));
                sink.AddLine(new Vector2(0 + frontRectWidth, 0 + _height / 2));
                sink.AddLine(new Vector2(0 - _backRectWidth, 0 + _height / 2));
                sink.EndFigure(FigureEnd.Closed);
                sink.AddCircleFigure(0, 0, _height * CapRadius / 2);
                sink.Close();
            }

            return geometry;
        }

        private RectangleF GetAabbForDegree(float degree)
        {
            var maxSpread = Shadows.GaugeNeedleDrop.MaxSpread;
            var capRadius = _height * CapRadius / 2f;
            var localRight = _frontWidth;
            var localLeft = Math.Min(-_backRectWidth, -capRadius);
            var localTop = -capRadius;

            var cx = (localLeft + localRight) / 2f;
            var cy = (localTop + capRadius) / 2f;
            var halfW = (localRight - localLeft) / 2f;
            var halfH = (capRadius - localTop) / 2f;

            var rad = degree * Math.PI / 180.0;
            var sin = (float)Math.Sin(rad);
            var cos = (float)Math.Cos(rad);
            var absSin = Math.Abs(sin);
            var absCos = Math.Abs(cos);

            var cxRotated = cx * cos - cy * sin;
            var cyRotated = cx * sin + cy * cos;

            var newHalfW = halfW * absCos + halfH * absSin;
            var newHalfH = halfW * absSin + halfH * absCos;

            return new RectangleF(
                0 + cxRotated - maxSpread - newHalfW,
                0 + cyRotated - maxSpread - newHalfH,
                newHalfW * 2f + maxSpread * 2,
                newHalfH * 2f + maxSpread * 2
            );
        }
    }
}