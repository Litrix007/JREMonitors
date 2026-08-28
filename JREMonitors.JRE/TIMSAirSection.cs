using System;

namespace JREMonitors.JRE
{
    public readonly struct TIMSAirSection : IEquatable<TIMSAirSection>
    {
        public int StartLocation { get; }
        public int EndLocation { get; }
        public int HintOffset { get; }

        public TIMSAirSection(int startLocation, int endLocation, int hintOffset)
        {
            StartLocation = startLocation;
            EndLocation = endLocation;
            HintOffset = hintOffset;
        }

        public bool Equals(TIMSAirSection other)
        {
            return StartLocation == other.StartLocation && EndLocation == other.EndLocation;
        }

        public override bool Equals(object obj)
        {
            return obj is TIMSAirSection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartLocation, EndLocation);
        }
    }
}