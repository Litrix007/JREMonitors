using System;
using JREMonitors.Core.Constants;

namespace JREMonitors.JRE
{
    public struct TIMSSignalSection : IEquatable<TIMSSignalSection>
    {
        public double Location { get; }
        public string Name { get; }

        public TIMSSignalSection(double location, string name = null)
        {
            Location = location;
            Name = name ?? string.Empty;
        }

        public bool Equals(TIMSSignalSection other)
        {
            return Math.Abs(Location - other.Location) < Epsilons.DoubleEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is TIMSSignalSection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }
    }
}