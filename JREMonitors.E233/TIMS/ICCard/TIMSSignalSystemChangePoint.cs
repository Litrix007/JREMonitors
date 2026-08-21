using System;

namespace JREMonitors.E233.TIMS.ICCard
{
    public class TIMSSignalSystemChangePoint<TSignal> where TSignal : struct, Enum
    {
        public TIMSSignalSystemChangePoint(int startLocation, TSignal signalSystem)
        {
            StartLocation = startLocation;
            SignalSystem = signalSystem;
        }

        public double StartLocation { get; }
        public TSignal SignalSystem { get; }
    }
}