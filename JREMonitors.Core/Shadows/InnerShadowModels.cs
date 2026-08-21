using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using JREMonitors.Core.Constants;
using Vortice.Mathematics;

namespace JREMonitors.Core.Shadows
{
    public readonly struct OffsetInnerShadow : IEquatable<OffsetInnerShadow>
    {
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float Blur;
        public readonly Color4 Color;
        public readonly float Choke;

        public OffsetInnerShadow(float offsetX, float offsetY, float blur, Color4 color, float choke = 0)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            Blur = blur;
            Color = color;
            Choke = choke;
        }

        public bool Equals(OffsetInnerShadow other)
        {
            return OffsetX.Equals(other.OffsetX) && OffsetY.Equals(other.OffsetY) && Blur.Equals(other.Blur) &&
                   Color.Equals(other.Color) && Choke.Equals(other.Choke);
        }

        public float MaxSpread => (float)Math.Ceiling(Math.Max(Math.Abs(OffsetX), Math.Abs(OffsetY)) + 3 * Blur);

        public static OffsetInnerShadow operator *(OffsetInnerShadow shadow, float factor)
        {
            return new OffsetInnerShadow(shadow.OffsetX * factor, shadow.OffsetY * factor, shadow.Blur, shadow.Color,
                shadow.Choke);
        }

        public static OffsetInnerShadow operator /(OffsetInnerShadow shadow, float factor)
        {
            return new OffsetInnerShadow(shadow.OffsetX / factor, shadow.OffsetY / factor, shadow.Blur, shadow.Color,
                shadow.Choke);
        }

        public OffsetInnerShadow With(
            float? offsetX = null,
            float? offsetY = null,
            float? blur = null,
            Color4? color = null,
            float? choke = null)
        {
            return new OffsetInnerShadow(offsetX ?? OffsetX, offsetY ?? OffsetY, blur ?? Blur, color ?? Color,
                choke ?? Choke);
        }


        public static bool operator ==(OffsetInnerShadow left, OffsetInnerShadow right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OffsetInnerShadow left, OffsetInnerShadow right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is OffsetInnerShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OffsetX, OffsetY, Blur, Color, Choke);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct OffsetShadowConstants
    {
        [FieldOffset(0)] public Vector2 Offset;
        [FieldOffset(8)] public float Choke;
        [FieldOffset(16)] public Vector4 Color;

        public OffsetShadowConstants(OffsetInnerShadow shadow, float worldScale)
        {
            Offset = new Vector2(shadow.OffsetX * worldScale, shadow.OffsetY * worldScale);
            Choke = shadow.Choke;
            Color = shadow.Color;
        }
    }

    public class OffsetInnerShadowListComparer : IEqualityComparer<IReadOnlyList<OffsetInnerShadow>>
    {
        public static readonly OffsetInnerShadowListComparer Instance = new OffsetInnerShadowListComparer();

        public bool Equals(IReadOnlyList<OffsetInnerShadow> x, IReadOnlyList<OffsetInnerShadow> y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null || x.Count != y.Count) return false;
            for (var i = 0; i < x.Count; i++)
                if (!x[i].Equals(y[i]))
                    return false;
            return true;
        }

        public int GetHashCode(IReadOnlyList<OffsetInnerShadow> obj)
        {
            var hash = 17;
            for (var i = 0; i < obj.Count; i++) hash = hash * 31 + obj[i].GetHashCode();
            return hash;
        }
    }

    public readonly struct ImageInnerShadow : IEquatable<ImageInnerShadow>
    {
        public readonly float Blur;
        public readonly Color4 Color;
        public readonly float Choke;

        public ImageInnerShadow(float blur, Color4 color, float choke = 0)
        {
            Blur = blur;
            Color = color;
            Choke = choke;
        }

        public bool Equals(ImageInnerShadow other)
        {
            return Math.Abs(Blur - other.Blur) < Epsilons.FloatEpsilon && Color.Equals(other.Color) &&
                   Math.Abs(Choke - other.Choke) < Epsilons.FloatEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is ImageInnerShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Blur, Color, Choke);
        }

        public static bool operator ==(ImageInnerShadow left, ImageInnerShadow right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageInnerShadow left, ImageInnerShadow right)
        {
            return !left.Equals(right);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ImageShadowConstants
    {
        public float Blur;
        public float Choke;
        private Vector2 _pad;
        public Vector4 Color;
    }
}