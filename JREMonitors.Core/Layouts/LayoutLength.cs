using System;
using JREMonitors.Core.Constants;

namespace JREMonitors.Core.Layouts
{
    public readonly struct LayoutLength : IEquatable<LayoutLength>
    {
        public readonly float AbsoluteValue;
        public readonly float FlexValue;

        public LayoutLength(float absoluteValue, float flexValue)
        {
            AbsoluteValue = absoluteValue;
            FlexValue = flexValue;
        }

        public static LayoutLength Absolute(float value)
        {
            return new LayoutLength(value, 0f);
        }

        public static LayoutLength Flex(float value = 1f)
        {
            return new LayoutLength(0f, value);
        }

        public bool Equals(LayoutLength other)
        {
            return Math.Abs(AbsoluteValue - other.AbsoluteValue) <= Epsilons.FloatEpsilon &&
                   Math.Abs(FlexValue - other.FlexValue) <= Epsilons.FloatEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is LayoutLength other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AbsoluteValue, FlexValue);
        }

        public static bool operator ==(LayoutLength left, LayoutLength right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LayoutLength left, LayoutLength right)
        {
            return !left.Equals(right);
        }
    }
}