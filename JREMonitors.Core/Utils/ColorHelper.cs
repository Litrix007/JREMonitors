using System;
using Vortice.Mathematics;

namespace JREMonitors.Core.Utils
{
    public static class ColorHelper
    {
        private static void GetRgbaFromHex(string hex, bool allowAlpha, out float r, out float g, out float b,
            out float a)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            switch (hex.Length)
            {
                // RGB
                case 3:
                {
                    r = Convert.ToByte(hex.Substring(0, 1), 16) / 15f;
                    g = Convert.ToByte(hex.Substring(1, 1), 16) / 15f;
                    b = Convert.ToByte(hex.Substring(2, 1), 16) / 15f;
                    a = 1;
                    break;
                }
                // RRGGBB
                case 6:
                {
                    r = Convert.ToByte(hex.Substring(0, 2), 16) / 255f;
                    g = Convert.ToByte(hex.Substring(2, 2), 16) / 255f;
                    b = Convert.ToByte(hex.Substring(4, 2), 16) / 255f;
                    a = 1;
                    break;
                }
                // AARRGGBB
                case 8 when allowAlpha:
                {
                    a = Convert.ToByte(hex.Substring(0, 2), 16) / 255f;
                    r = Convert.ToByte(hex.Substring(2, 2), 16) / 255f;
                    g = Convert.ToByte(hex.Substring(4, 2), 16) / 255f;
                    b = Convert.ToByte(hex.Substring(6, 2), 16) / 255f;
                    break;
                }
                default:
                    throw new FormatException("Invalid color format");
            }
        }

        public static Color4 ToColor4(this string hex)
        {
            GetRgbaFromHex(hex, true, out var r, out var g, out var b, out var a);
            return new Color4(r, g, b, a);
        }

        public static Color3 ToColor3(this string hex)
        {
            GetRgbaFromHex(hex, false, out var r, out var g, out var b, out _);
            return new Color3(r, g, b);
        }

        public static Color3 ToColor3(this Color4 color)
        {
            return new Color3(color.R, color.G, color.B);
        }


        public static Color4 ToColor4(this Color3 color, float alpha = 1)
        {
            return new Color4(color.R, color.G, color.B, alpha);
        }

        public static Color4 WithAlpha(this Color4 color, float alpha)
        {
            return new Color4(color.R, color.G, color.B, alpha);
        }

        public static Color4 MultiplyRgb(this Color4 color, float factor)
        {
            return new Color4(color.R * factor, color.G * factor, color.B * factor, color.A);
        }

        public static Color4 MultiplyAlpha(this Color4 color, float factor)
        {
            return new Color4(color.R, color.G, color.B, color.A * factor);
        }

        public static Color4 Inverted(this Color4 color)
        {
            return new Color4(
                1f - color.R,
                1f - color.G,
                1f - color.B,
                color.A
            );
        }
    }
}