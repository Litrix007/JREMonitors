using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JREMonitors.Core.Utils;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Svg
{
    public static class SvgParser
    {
        private static readonly Regex TransformRegex = new Regex(@"([a-zA-Z]+)\s*\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex PathRegex = new Regex(@"([a-zA-Z])|([-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?)",
            RegexOptions.Compiled);

        public static float? ParseFloat(string s)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (float?)null;
        }

        public static bool ParseDisplay(XElement el)
        {
            var display = el.Attribute("display")?.Value ?? ExtractStyle(el, "display");
            return display != "none";
        }

        public static string ExtractStyle(XElement el, string key)
        {
            var style = el.Attribute("style")?.Value;
            if (string.IsNullOrEmpty(style)) return null;
            var match = Regex.Match(style, $@"{key}\s*:\s*([^;]+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        public static Color4? ParseColor(string c)
        {
            if (string.IsNullOrEmpty(c) || c == "none") return null;
            if (c.StartsWith("#")) return c.ToColor4();

            return null;
        }

        public static CapStyle? ParseStrokeLineCap(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var sLower = s.Trim().ToLower();
            switch (sLower)
            {
                case "round":
                    return CapStyle.Round;
                case "square":
                    return CapStyle.Square;
                case "butt":
                    return CapStyle.Flat;
                default:
                    return null;
            }
        }

        public static Matrix3x2 ParseTransform(string transformStr)
        {
            if (string.IsNullOrEmpty(transformStr)) return Matrix3x2.Identity;
            var matrix = Matrix3x2.Identity;
            var matches = TransformRegex.Matches(transformStr);
            foreach (Match m in matches)
            {
                var cmd = m.Groups[1].Value.ToLower();
                var args = m.Groups[2].Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => ParseFloat(s) ?? 0f).ToArray();
                switch (cmd)
                {
                    case "translate" when args.Length >= 1:
                        matrix *= Matrix3x2.CreateTranslation(args[0], args.Length > 1 ? args[1] : 0f);
                        break;
                    case "scale" when args.Length >= 1:
                    {
                        var sx = args[0];
                        var sy = args.Length > 1 ? args[1] : sx;
                        matrix *= Matrix3x2.CreateScale(sx, sy);
                        break;
                    }
                    case "rotate" when args.Length >= 1:
                    {
                        var cx = args.Length >= 3 ? args[1] : 0f;
                        var cy = args.Length >= 3 ? args[2] : 0f;
                        matrix *= Matrix3x2.CreateRotation(args[0] * ((float)Math.PI / 180f), new Vector2(cx, cy));
                        break;
                    }
                }
            }

            return matrix;
        }

        public static void ParsePathAndFill(string d, ID2D1PathGeometry geometry)
        {
            if (string.IsNullOrEmpty(d)) return;

            var matches = PathRegex.Matches(d);

            using (var sink = geometry.Open())
            {
                var index = 0;
                var currentCmd = ' ';
                var currentPoint = Vector2.Zero;
                var startFigurePoint = Vector2.Zero;
                // 跟踪上一次的贝塞尔控制点
                var lastControlPoint = Vector2.Zero;
                var inFigure = false;

                float ReadNext()
                {
                    return float.Parse(matches[index++].Groups[2].Value, CultureInfo.InvariantCulture);
                }

                bool HasNext()
                {
                    return index < matches.Count;
                }

                while (HasNext())
                {
                    if (matches[index].Groups[1].Success) currentCmd = matches[index++].Groups[1].Value[0];

                    var isRelative = char.IsLower(currentCmd);
                    var cmdUpper = char.ToUpper(currentCmd);

                    float x, y;

                    switch (cmdUpper)
                    {
                        case 'M':
                            x = ReadNext();
                            y = ReadNext();
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);
                            if (inFigure) sink.EndFigure(FigureEnd.Open);
                            sink.BeginFigure(currentPoint, FigureBegin.Filled);
                            startFigurePoint = currentPoint;
                            inFigure = true;
                            currentCmd = isRelative ? 'l' : 'L';
                            lastControlPoint = currentPoint;
                            break;

                        case 'L':
                            x = ReadNext();
                            y = ReadNext();
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);
                            sink.AddLine(currentPoint);
                            lastControlPoint = currentPoint;
                            break;

                        case 'H':
                            x = ReadNext();
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y)
                                : new Vector2(x, currentPoint.Y);
                            sink.AddLine(currentPoint);
                            lastControlPoint = currentPoint;
                            break;

                        case 'V':
                            y = ReadNext();
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X, currentPoint.Y + y)
                                : new Vector2(currentPoint.X, y);
                            sink.AddLine(currentPoint);
                            lastControlPoint = currentPoint;
                            break;

                        case 'C':
                        {
                            var x1 = ReadNext();
                            var y1 = ReadNext();
                            var x2 = ReadNext();
                            var y2 = ReadNext();
                            x = ReadNext();
                            y = ReadNext();

                            var pt1 = isRelative
                                ? new Vector2(currentPoint.X + x1, currentPoint.Y + y1)
                                : new Vector2(x1, y1);
                            var pt2 = isRelative
                                ? new Vector2(currentPoint.X + x2, currentPoint.Y + y2)
                                : new Vector2(x2, y2);
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);

                            sink.AddBezier(new BezierSegment { Point1 = pt1, Point2 = pt2, Point3 = currentPoint });
                            lastControlPoint = pt2;
                            break;
                        }

                        case 'S':
                        {
                            var x2 = ReadNext();
                            var y2 = ReadNext();
                            x = ReadNext();
                            y = ReadNext();
                            // 反射前一个控制点（如果前一个不是C或S，lastControlPoint应该等于currentPoint，相减抵消）
                            var pt1 = new Vector2(2 * currentPoint.X - lastControlPoint.X,
                                2 * currentPoint.Y - lastControlPoint.Y);
                            var pt2 = isRelative
                                ? new Vector2(currentPoint.X + x2, currentPoint.Y + y2)
                                : new Vector2(x2, y2);
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);

                            sink.AddBezier(new BezierSegment { Point1 = pt1, Point2 = pt2, Point3 = currentPoint });
                            lastControlPoint = pt2;
                            break;
                        }

                        case 'Q':
                        {
                            var x1 = ReadNext();
                            var y1 = ReadNext();
                            x = ReadNext();
                            y = ReadNext();

                            var pt1 = isRelative
                                ? new Vector2(currentPoint.X + x1, currentPoint.Y + y1)
                                : new Vector2(x1, y1);
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);

                            sink.AddQuadraticBezier(new QuadraticBezierSegment { Point1 = pt1, Point2 = currentPoint });
                            lastControlPoint = pt1;
                            break;
                        }

                        case 'T':
                        {
                            x = ReadNext();
                            y = ReadNext();

                            var pt1 = new Vector2(2 * currentPoint.X - lastControlPoint.X,
                                2 * currentPoint.Y - lastControlPoint.Y);
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);

                            sink.AddQuadraticBezier(new QuadraticBezierSegment { Point1 = pt1, Point2 = currentPoint });
                            lastControlPoint = pt1;
                            break;
                        }

                        case 'A':
                            var rx = ReadNext();
                            var ry = ReadNext();
                            var rot = ReadNext();
                            var largeArc = (int)ReadNext();
                            var sweep = (int)ReadNext();
                            x = ReadNext();
                            y = ReadNext();
                            currentPoint = isRelative
                                ? new Vector2(currentPoint.X + x, currentPoint.Y + y)
                                : new Vector2(x, y);
                            sink.AddArc(new ArcSegment
                            {
                                Point = currentPoint,
                                Size = new SizeF(rx, ry),
                                RotationAngle = rot,
                                ArcSize = largeArc == 1 ? ArcSize.Large : ArcSize.Small,
                                SweepDirection = sweep == 1 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise
                            });
                            lastControlPoint = currentPoint;
                            break;

                        case 'Z':
                            if (inFigure)
                            {
                                sink.EndFigure(FigureEnd.Closed);
                                inFigure = false;
                                currentPoint = startFigurePoint;
                            }

                            lastControlPoint = currentPoint;
                            break;

                        default:
                            // 遇到未知指令时，强行跳过后面的数字参数以防死循环
                            while (HasNext() && !matches[index].Groups[1].Success) index++;

                            lastControlPoint = currentPoint;
                            break;
                    }
                }

                if (inFigure) sink.EndFigure(FigureEnd.Open);
                sink.Close();
            }
        }
    }
}