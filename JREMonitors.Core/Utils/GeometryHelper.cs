using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Utils
{
    public struct RowItemWidth
    {
        public readonly int Count;
        public readonly float Width;

        public RowItemWidth(int count, float width)
        {
            Count = count;
            Width = width;
        }
    }

    public static class GeometryHelper
    {
        public static readonly StrokeStyleProperties1 SquareStrokeStyleProperties = new StrokeStyleProperties1
        {
            StartCap = CapStyle.Square,
            EndCap = CapStyle.Square,
            LineJoin = LineJoin.Miter,
            MiterLimit = 1.0f
        };

        public static Vector2 ForwardByAngle(this Vector2 startPoint, float angleInDegrees, float length)
        {
            var radians = angleInDegrees * (MathHelper.Pi / 180f);
            var offsetX = length * MathHelper.Cos(radians);
            var offsetY = length * MathHelper.Sin(radians);
            return new Vector2(startPoint.X + offsetX, startPoint.Y + offsetY);
        }

        public static PointF ToPointF(this Vector2 v)
        {
            return new PointF(v.X, v.Y);
        }

        public static Vector2 ToVector2(this PointF p)
        {
            return new Vector2(p.X, p.Y);
        }

        public static Vector2 ToVector2(this SizeF p)
        {
            return new Vector2(p.Width, p.Height);
        }

        public static RectangleF SnapToPixels(this RectangleF rect, bool snapSizeAbsolute = true)
        {
            var x = (float)Math.Floor(rect.X);
            var y = (float)Math.Floor(rect.Y);
            if (snapSizeAbsolute)
            {
                var right = (float)Math.Ceiling(rect.Right);
                var bottom = (float)Math.Ceiling(rect.Bottom);
                return new RectangleF(x, y, right - x, bottom - y);
            }

            return new RectangleF(x, y, (float)Math.Ceiling(rect.Width), (float)Math.Ceiling(rect.Height));
        }

        public static RectangleF CreateRowBounds(
            float x, float y,
            float spacing,
            IList<RowItemWidth> widths,
            float height,
            float horizontalAlignment = 0,
            float verticalAlignment = 0
        )
        {
            var contentWidth = widths.Sum(w => w.Width * w.Count);
            var totalSpacing = spacing * (widths.Sum(w => w.Count) - 1);
            var totalWidth = contentWidth + totalSpacing;
            return new RectangleF(x - totalWidth * horizontalAlignment, y - height * verticalAlignment, totalWidth,
                height);
        }

        public static RectangleF CreateGridBounds(
            float x,
            float y,
            float rowSpacing,
            float colSpacing,
            float itemWidth,
            float itemHeight,
            float row,
            int col,
            float horizontalAlignment = 0,
            float verticalAlignment = 0
        )
        {
            var width = itemWidth * col + colSpacing * (col - 1);
            var height = itemHeight * row + rowSpacing * (row - 1);
            return new RectangleF(x - width * horizontalAlignment, y - height * verticalAlignment, width, height);
        }

        public static void AddCircleFigure(this ID2D1GeometrySink sink, float x, float y, float radius)
        {
            sink.SetFillMode(FillMode.Winding);
            var top = new Vector2(x, y - radius);
            var bottom = new Vector2(x, y + radius);
            sink.BeginFigure(top, FigureBegin.Filled);
            sink.AddArc(new ArcSegment
            {
                Point = bottom,
                Size = new SizeF(radius, radius),
                RotationAngle = 0f,
                SweepDirection = SweepDirection.Clockwise,
                ArcSize = ArcSize.Small
            });
            sink.AddArc(new ArcSegment
            {
                Point = top,
                Size = new SizeF(radius, radius),
                RotationAngle = 0f,
                SweepDirection = SweepDirection.Clockwise,
                ArcSize = ArcSize.Small
            });
            sink.EndFigure(FigureEnd.Closed);
        }

        public static void AddRoundedCorner(this ID2D1GeometrySink sink, Vector2 begin, Vector2 end, bool isClockwise)
        {
            var radiusX = MathHelper.Abs(end.X - begin.X);
            var radiusY = MathHelper.Abs(end.Y - begin.Y);

            if (radiusX == 0 || radiusY == 0)
            {
                sink.AddLine(end);
                return;
            }

            var sweepDirection = isClockwise ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

            var arcSegment = new ArcSegment
            {
                Point = end,
                Size = new SizeF(radiusX, radiusY),
                RotationAngle = 0f,
                SweepDirection = sweepDirection,
                ArcSize = ArcSize.Small
            };
            sink.AddArc(arcSegment);
        }

        public static void AddRoundedPolygonFigure(
            this ID2D1GeometrySink sink,
            Vector2[] vertices,
            float[] radii,
            bool[] isTopOrLeft = null)
        {
            if (vertices == null || vertices.Length < 3)
                throw new ArgumentException("Polygon must have at least 3 vertices to form a shape.");

            if (radii == null) throw new ArgumentNullException(nameof(radii));

            var n = vertices.Length;
            Span<Vector2> t1Points = stackalloc Vector2[n];
            Span<Vector2> t2Points = stackalloc Vector2[n];
            Span<float> actualRadii = stackalloc float[n];
            Span<SweepDirection> sweepDirections = stackalloc SweepDirection[n];

            for (var i = 0; i < n; i++)
            {
                var corner = vertices[i];
                var prev = vertices[(i - 1 + n) % n];
                var next = vertices[(i + 1) % n];

                var v1 = prev - corner;
                var v2 = next - corner;

                var len1 = v1.Length();
                var len2 = v2.Length();

                var desiredRadius = i < radii.Length ? radii[i] : 0f;

                if (desiredRadius <= 0.1f || len1 < Epsilons.FloatEpsilon || len2 < Epsilons.FloatEpsilon)
                {
                    t1Points[i] = corner;
                    t2Points[i] = corner;
                    actualRadii[i] = 0;
                    continue;
                }

                var n1 = v1 / len1;
                var n2 = v2 / len2;

                var dot = Vector2.Dot(n1, n2);
                dot = Math.Max(-1f, Math.Min(1f, dot));

                if (Math.Abs(dot + 1f) < Epsilons.FloatEpsilon)
                {
                    t1Points[i] = corner;
                    t2Points[i] = corner;
                    actualRadii[i] = 0;
                    continue;
                }

                var theta = (float)Math.Acos(dot);
                var currentRadius = desiredRadius;
                var d = currentRadius / (float)Math.Tan(theta / 2f);

                var maxD = Math.Min(len1, len2) * 0.5f;
                if (d > maxD)
                {
                    d = maxD;
                    currentRadius = d * (float)Math.Tan(theta / 2f);
                }

                t1Points[i] = corner + n1 * d;
                t2Points[i] = corner + n2 * d;
                actualRadii[i] = currentRadius;

                var cross = n1.X * n2.Y - n1.Y * n2.X;
                if (isTopOrLeft != null && i < isTopOrLeft.Length)
                    sweepDirections[i] = isTopOrLeft[i] ? SweepDirection.CounterClockwise : SweepDirection.Clockwise;
                else
                    sweepDirections[i] = cross < 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
            }

            sink.BeginFigure(t2Points[n - 1], FigureBegin.Filled);

            for (var i = 0; i < n; i++)
            {
                sink.AddLine(t1Points[i]);

                if (actualRadii[i] > 0.1f)
                {
                    var arc = new ArcSegment
                    {
                        Point = t2Points[i],
                        Size = new SizeF(actualRadii[i], actualRadii[i]),
                        RotationAngle = 0,
                        SweepDirection = sweepDirections[i],
                        ArcSize = ArcSize.Small
                    };
                    sink.AddArc(arc);
                }
                else
                {
                    sink.AddLine(t2Points[i]);
                }
            }

            sink.EndFigure(FigureEnd.Closed);
        }

        public static void AddArcLineFigure(this ID2D1GeometrySink sink, float radius,
            float startAngleInDegrees,
            float sweepAngleInDegrees)
        {
            var startRad = startAngleInDegrees * (MathHelper.Pi / 180);
            var endRad = (startAngleInDegrees + sweepAngleInDegrees) * (MathHelper.Pi / 180);
            var startPoint = new Vector2(radius * MathHelper.Cos(startRad),
                radius * MathHelper.Sin(startRad));
            var endPoint = new Vector2(radius * MathHelper.Cos(endRad), radius * MathHelper.Sin(endRad));
            sink.BeginFigure(startPoint, FigureBegin.Hollow);
            var arc = new ArcSegment
            {
                Point = endPoint,
                Size = new SizeF(radius, radius),
                RotationAngle = 0,
                SweepDirection = sweepAngleInDegrees > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise,
                ArcSize = MathHelper.Abs(sweepAngleInDegrees) > 180 ? ArcSize.Large : ArcSize.Small
            };
            sink.AddArc(arc);
            sink.EndFigure(FigureEnd.Open);
        }

        public static void AddFigureFromPoints(this ID2D1GeometrySink sink, IList<Vector2> points,
            FigureBegin figureBegin = FigureBegin.Filled,
            FigureEnd figureEnd = FigureEnd.Closed)
        {
            if (points == null || points.Count == 0) return;
            sink.BeginFigure(points[0], figureBegin);
            for (var i = 1; i < points.Count; i++) sink.AddLine(points[i]);
            sink.EndFigure(figureEnd);
        }

        public static void AddTopLeftInnerBevelFigure(
            this ID2D1GeometrySink sink,
            float width,
            float height,
            float bevelWidth,
            float borderRadius,
            float blurMargin,
            float endInset = 0f
        )
        {
            var d = Math.Max(0, bevelWidth);
            var b = Math.Max(0, blurMargin);
            var inset = Math.Max(0, endInset);
            var maxD = Math.Max(0, Math.Min((width - 2 * borderRadius) / 2f, (height - 2 * borderRadius) / 2f));
            if (d > maxD) d = maxD;
            var trInnerX = Math.Max(d, width - d - inset);
            var trOuterX = trInnerX + d + b;
            var blInnerY = Math.Min(height - d, height - d - inset);
            var blOuterY = blInnerY + d + b;
            var arcStart = new Vector2(Math.Max(d, borderRadius), d);
            var arcEnd = new Vector2(d, Math.Max(d, borderRadius));
            sink.BeginFigure(new Vector2(-b, blOuterY), FigureBegin.Filled);
            sink.AddLine(new Vector2(-b, -b));
            sink.AddLine(new Vector2(trOuterX, -b));
            sink.AddLine(new Vector2(trInnerX, d));
            sink.AddLine(arcStart);
            sink.AddRoundedCorner(arcStart, arcEnd, false);
            sink.AddLine(new Vector2(d, Math.Min(height - d, height - borderRadius)));
            sink.AddLine(new Vector2(d, blInnerY));
            sink.AddLine(new Vector2(-b, blOuterY));
            sink.EndFigure(FigureEnd.Closed);
        }

        public static void AddBottomRightInnerBevelFigure(
            this ID2D1GeometrySink sink,
            float width,
            float height,
            float bevelWidth,
            float borderRadius,
            float blurMargin,
            float endInset = 0f
        )
        {
            var d = Math.Max(0, bevelWidth);
            var b = Math.Max(0, blurMargin);
            var inset = Math.Max(0, endInset);
            var maxD = Math.Max(0, Math.Min((width - 2 * borderRadius) / 2f, (height - 2 * borderRadius) / 2f));
            if (d > maxD) d = maxD;
            var trInnerY = Math.Min(height - d, d + inset);
            var trOuterY = trInnerY - d - b;
            var blInnerX = Math.Max(d, d + inset);
            var blOuterX = blInnerX - d - b;
            var arcStart = new Vector2(Math.Min(width - d, width - borderRadius), height - d);
            var arcEnd = new Vector2(width - d, Math.Min(height - d, height - borderRadius));
            sink.BeginFigure(new Vector2(width + b, trOuterY), FigureBegin.Filled);
            sink.AddLine(new Vector2(width + b, height + b));
            sink.AddLine(new Vector2(blOuterX, height + b));
            sink.AddLine(new Vector2(blInnerX, height - d));
            sink.AddLine(arcStart);
            sink.AddRoundedCorner(arcStart, arcEnd, false);
            sink.AddLine(new Vector2(width - d, Math.Max(d, borderRadius)));
            sink.AddLine(new Vector2(width - d, trInnerY));
            sink.AddLine(new Vector2(width + b, trOuterY));
            sink.EndFigure(FigureEnd.Closed);
        }

        public static bool IsAngleBetween(float target, float start, float sweep)
        {
            start = (start % 360f + 360f) % 360f;
            target = (target % 360f + 360f) % 360f;

            var targetRel = (target - start + 360f) % 360f;

            if (sweep >= 0) return targetRel <= sweep;

            var positiveSweep = -sweep;
            var startCcw = (start + sweep % 360f + 360f) % 360f;
            var targetRelCcw = (target - startCcw + 360f) % 360f;
            return targetRelCcw <= positiveSweep;
        }

        public static RectangleF GetArcBounds(float innerRadius, float outerRadius, float startAngle, float sweepAngle)
        {
            var startRad = startAngle * (MathHelper.Pi / 180);
            var endRad = (startAngle + sweepAngle) * (MathHelper.Pi / 180);
            var x1 = outerRadius * MathHelper.Cos(startRad);
            var y1 = outerRadius * MathHelper.Sin(startRad);
            var x2 = outerRadius * MathHelper.Cos(endRad);
            var y2 = outerRadius * MathHelper.Sin(endRad);
            var x3 = innerRadius * MathHelper.Cos(startRad);
            var y3 = innerRadius * MathHelper.Sin(startRad);
            var x4 = innerRadius * MathHelper.Cos(endRad);
            var y4 = innerRadius * MathHelper.Sin(endRad);
            var minX = MathHelper.Min(MathHelper.Min(x1, x2), MathHelper.Min(x3, x4));
            var maxX = MathHelper.Max(MathHelper.Max(x1, x2), MathHelper.Max(x3, x4));
            var minY = MathHelper.Min(MathHelper.Min(y1, y2), MathHelper.Min(y3, y4));
            var maxY = MathHelper.Max(MathHelper.Max(y1, y2), MathHelper.Max(y3, y4));
            if (IsAngleBetween(0f, startAngle, sweepAngle)) maxX = MathHelper.Max(maxX, outerRadius);
            if (IsAngleBetween(90f, startAngle, sweepAngle)) maxY = MathHelper.Max(maxY, outerRadius);
            if (IsAngleBetween(180f, startAngle, sweepAngle)) minX = MathHelper.Min(minX, -outerRadius);
            if (IsAngleBetween(270f, startAngle, sweepAngle)) minY = MathHelper.Min(minY, -outerRadius);
            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }
    }
}