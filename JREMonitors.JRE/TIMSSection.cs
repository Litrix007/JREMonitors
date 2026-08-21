using System;
using JREMonitors.Core.Constants;

namespace JREMonitors.JRE
{
    public struct TIMSSection : IEquatable<TIMSSection>
    {
        public double Location { get; }
        public string Name { get; }

        public TIMSSection(double location, string name = null)
        {
            Location = location;
            Name = name ?? string.Empty;
        }

        public bool Equals(TIMSSection other)
        {
            return Math.Abs(Location - other.Location) < Epsilons.DoubleEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is TIMSSection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }
    }
}