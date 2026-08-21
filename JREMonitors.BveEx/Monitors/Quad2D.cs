using System;
using System.Numerics;
using JREMonitors.Core.Constants;

namespace JREMonitors.BveEx.Monitors
{
    public struct Quad2D
    {
        public Vector2 TopLeft { get; set; }
        public Vector2 TopRight { get; set; }
        public Vector2 BottomLeft { get; set; }
        public Vector2 BottomRight { get; set; }

        public Quad2D(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public void GetAabb(out float minX, out float maxX, out float minY, out float maxY, out int aabbWidth,
            out int aabbHeight)
        {
            minX = Math.Min(Math.Min(TopLeft.X, TopRight.X), Math.Min(BottomLeft.X, BottomRight.X));
            maxX = Math.Max(Math.Max(TopLeft.X, TopRight.X), Math.Max(BottomLeft.X, BottomRight.X));
            minY = Math.Min(Math.Min(TopLeft.Y, TopRight.Y), Math.Min(BottomLeft.Y, BottomRight.Y));
            maxY = Math.Max(Math.Max(TopLeft.Y, TopRight.Y), Math.Max(BottomLeft.Y, BottomRight.Y));
            aabbWidth = (int)Math.Ceiling(maxX - minX);
            aabbHeight = (int)Math.Ceiling(maxY - minY);
        }

        public void GetScaledAabb(
            float srcWidth,
            float srcHeight,
            out float minX,
            out float minY,
            out int scaledAabbWidth,
            out int scaledAabbHeight,
            out int originalAabbWidth,
            out int originalAabbHeight
        )
        {
            GetAabb(out minX, out _, out minY, out _, out originalAabbWidth,
                out originalAabbHeight);
            var scaleX = srcWidth / originalAabbWidth;
            var scaleY = srcHeight / originalAabbHeight;
            scaledAabbWidth = (int)Math.Ceiling(originalAabbWidth * scaleX);
            scaledAabbHeight = (int)Math.Ceiling(originalAabbHeight * scaleY);
        }

        /// <summary>
        ///     计算单应性矩阵
        /// </summary>
        public Matrix4x4 ToHomographyMatrix(float srcWidth, float srcHeight)
        {
            var p0 = TopLeft;
            var p1 = TopRight;
            var p2 = BottomRight;
            var p3 = BottomLeft;
            var dx1 = p1.X - p2.X;
            var dx2 = p3.X - p2.X;
            var dy1 = p1.Y - p2.Y;
            var dy2 = p3.Y - p2.Y;
            var sumX = p0.X - p1.X + p2.X - p3.X;
            var sumY = p0.Y - p1.Y + p2.Y - p3.Y;
            float a, b, c, d, e, f, g, h;
            if (Math.Abs(sumX) < Epsilons.FloatEpsilon && Math.Abs(sumY) < Epsilons.FloatEpsilon)
            {
                a = p1.X - p0.X;
                b = p2.X - p1.X;
                c = p0.X;
                d = p1.Y - p0.Y;
                e = p2.Y - p1.Y;
                f = p0.Y;
                g = 0f;
                h = 0f;
            }
            else
            {
                var det = dx1 * dy2 - dx2 * dy1;
                if (Math.Abs(det) < Epsilons.FloatEpsilon) return Matrix4x4.Identity;

                g = (sumX * dy2 - sumY * dx2) / det;
                h = (dx1 * sumY - dy1 * sumX) / det;

                a = p1.X - p0.X + g * p1.X;
                b = p3.X - p0.X + h * p3.X;
                c = p0.X;

                d = p1.Y - p0.Y + g * p1.Y;
                e = p3.Y - p0.Y + h * p3.Y;
                f = p0.Y;
            }

            var aFinal = a / srcWidth;
            var bFinal = b / srcHeight;
            var cFinal = c;
            var dFinal = d / srcWidth;
            var eFinal = e / srcHeight;
            var fFinal = f;
            var gFinal = g / srcWidth;
            var hFinal = h / srcHeight;
            return new Matrix4x4(
                aFinal, dFinal, 0f, gFinal,
                bFinal, eFinal, 0f, hFinal,
                0f, 0f, 1f, 0f,
                cFinal, fFinal, 0f, 1f
            );
        }

        /// <summary>
        ///     判断四边形是否为凸四边形
        /// </summary>
        public bool IsConvex()
        {
            var p0 = TopLeft;
            var p1 = TopRight;
            var p2 = BottomRight;
            var p3 = BottomLeft;
            var e0 = p1 - p0;
            var e1 = p2 - p1;
            var e2 = p3 - p2;
            var e3 = p0 - p3;
            var c0 = e0.X * e1.Y - e0.Y * e1.X;
            var c1 = e1.X * e2.Y - e1.Y * e2.X;
            var c2 = e2.X * e3.Y - e2.Y * e3.X;
            var c3 = e3.X * e0.Y - e3.Y * e0.X;
            return (c0 > Epsilons.FloatEpsilon && c1 > Epsilons.FloatEpsilon && c2 > Epsilons.FloatEpsilon &&
                    c3 > Epsilons.FloatEpsilon) ||
                   (c0 < -Epsilons.FloatEpsilon && c1 < -Epsilons.FloatEpsilon && c2 < -Epsilons.FloatEpsilon &&
                    c3 < -Epsilons.FloatEpsilon);
        }

        public bool IsAabbEmpty()
        {
            GetAabb(out _, out _, out _, out _, out var aabbWidth, out var aabbHeight);
            return aabbWidth <= 0 || aabbHeight <= 0;
        }

        /// <summary>
        ///     判断点是否在凸四边形内部
        /// </summary>
        public bool Contains(Vector2 p)
        {
            var p0 = TopLeft;
            var p1 = TopRight;
            var p2 = BottomRight;
            var p3 = BottomLeft;

            var d0 = (p1.X - p0.X) * (p.Y - p0.Y) - (p1.Y - p0.Y) * (p.X - p0.X);
            var d1 = (p2.X - p1.X) * (p.Y - p1.Y) - (p2.Y - p1.Y) * (p.X - p1.X);
            var d2 = (p3.X - p2.X) * (p.Y - p2.Y) - (p3.Y - p2.Y) * (p.X - p2.X);
            var d3 = (p0.X - p3.X) * (p.Y - p3.Y) - (p0.Y - p3.Y) * (p.X - p3.X);

            return (d0 >= -Epsilons.FloatEpsilon && d1 >= -Epsilons.FloatEpsilon && d2 >= -Epsilons.FloatEpsilon &&
                    d3 >= -Epsilons.FloatEpsilon) ||
                   (d0 <= Epsilons.FloatEpsilon && d1 <= Epsilons.FloatEpsilon && d2 <= Epsilons.FloatEpsilon &&
                    d3 <= Epsilons.FloatEpsilon);
        }
    }
}