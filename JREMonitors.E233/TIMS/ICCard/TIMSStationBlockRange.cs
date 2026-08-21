using System;
using JREMonitors.Core.Constants;

namespace JREMonitors.E233.TIMS.ICCard
{
    public struct TIMSStationBlockRange : IEquatable<TIMSStationBlockRange>
    {
        public double StartLocation { get; }
        public double EndLocation { get; }

        public TIMSStationBlockRange(double startLocation, double endLocation)
        {
            StartLocation = startLocation;
            EndLocation = endLocation;
        }

        public bool Equals(TIMSStationBlockRange other)
        {
            return Math.Abs(StartLocation - other.StartLocation) < Epsilons.DoubleEpsilon &&
                   Math.Abs(EndLocation - other.EndLocation) < Epsilons.DoubleEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is TIMSStationBlockRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartLocation, EndLocation);
        }
    }
}