using System;
using JREMonitors.Core.Constants;
using Vortice.Mathematics;

namespace JREMonitors.Core.Shadows
{
    public struct DropShadow : IEquatable<DropShadow>
    {
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float BlurX { get; set; }
        public float BlurY { get; set; }
        public Color4 Color { get; set; }

        public DropShadow(float offsetX, float offsetY, float blurX, float blurY, Color4 color)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            BlurX = blurX;
            BlurY = blurY;
            Color = color;
        }

        public float MaxSpread =>
            (float)Math.Ceiling(Math.Max(Math.Abs(OffsetX), Math.Abs(OffsetY)) + 3 * Math.Max(BlurX, BlurY));

        public static DropShadow operator *(DropShadow dropShadow, float factor)
        {
            var newShadow = dropShadow;
            newShadow.OffsetX *= factor;
            newShadow.OffsetY *= factor;
            return newShadow;
        }

        public static DropShadow operator /(DropShadow dropShadow, float factor)
        {
            var newShadow = dropShadow;
            newShadow.OffsetX /= factor;
            newShadow.OffsetY /= factor;
            return newShadow;
        }

        public bool Equals(DropShadow other)
        {
            return Math.Abs(OffsetX - other.OffsetX) < Epsilons.FloatEpsilon
                   && Math.Abs(OffsetY - other.OffsetY) < Epsilons.FloatEpsilon &&
                   Math.Abs(BlurX - other.BlurX) < Epsilons.FloatEpsilon
                   && Math.Abs(BlurY - other.BlurY) < Epsilons.FloatEpsilon
                   && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            return obj is DropShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OffsetX, OffsetY, BlurX, BlurY, Color);
        }
    }
}