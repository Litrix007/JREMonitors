using System;

namespace JREMonitors.E233.TIMS.ICCard
{
    public struct TIMSSlowSection : IEquatable<TIMSSlowSection>
    {
        public int StartMileage { get; }
        public int EndMileage { get; }
        public int SpeedLimit { get; }

        public TIMSSlowSection(int startMileage, int endMileage, int speedLimit)
        {
            StartMileage = startMileage;
            EndMileage = endMileage;
            SpeedLimit = speedLimit;
        }

        public bool Equals(TIMSSlowSection other)
        {
            return StartMileage == other.StartMileage && EndMileage == other.EndMileage;
        }

        public override bool Equals(object obj)
        {
            return obj is TIMSSlowSection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartMileage, EndMileage);
        }
    }
}