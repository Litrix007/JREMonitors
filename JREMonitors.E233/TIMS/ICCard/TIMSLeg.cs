using System;
using System.Collections.Generic;
using JREMonitors.JRE;

namespace JREMonitors.E233.TIMS.ICCard
{
    public class TIMSLeg<TSignal> where TSignal : struct, Enum
    {
        public TIMSLeg(
            string trainNumber,
            char? trainNumberChar,
            string formation,
            TIMSDisplayMode displayMode,
            TIMSDestination overrideDestination,
            TIMSNextDuty nextDuty,
            int switchDuration,
            IReadOnlyList<TIMSStation<TSignal>> stations,
            IReadOnlyList<TIMSSignalSystemChangePoint<TSignal>> signalSystemChangePoints,
            IReadOnlyList<TIMSMileageCorrectionPoint> mileageCorrectionPoints,
            IReadOnlyList<(int startLocation, int endLocation, int speedLimit)> slowSectionLocations,
            IReadOnlyList<TIMSAirSection> airSections)
        {
            TrainNumber = trainNumber;
            TrainNumberChar = trainNumberChar;
            Formation = formation;
            DisplayMode = displayMode;
            OverrideDestination = overrideDestination;
            NextDuty = nextDuty;
            SwitchDuration = switchDuration;
            Stations = stations;
            SignalSystemChangePoints = signalSystemChangePoints ?? Array.Empty<TIMSSignalSystemChangePoint<TSignal>>();
            MileageCorrectionPoints = mileageCorrectionPoints ?? Array.Empty<TIMSMileageCorrectionPoint>();
            SlowSectionLocations = slowSectionLocations ?? Array.Empty<(int, int, int)>();
            AirSections = airSections ?? Array.Empty<TIMSAirSection>();
        }

        public string TrainNumber { get; }
        public char? TrainNumberChar { get; }
        public string Formation { get; }
        public TIMSDisplayMode DisplayMode { get; }
        public TIMSDestination OverrideDestination { get; }
        public TIMSNextDuty NextDuty { get; }
        public int SwitchDuration { get; }
        public IReadOnlyList<TIMSStation<TSignal>> Stations { get; }
        public IReadOnlyList<TIMSSignalSystemChangePoint<TSignal>> SignalSystemChangePoints { get; }
        public IReadOnlyList<TIMSMileageCorrectionPoint> MileageCorrectionPoints { get; }
        public IReadOnlyList<(int startLocation, int endLocation, int speedLimit)> SlowSectionLocations { get; }
        public IReadOnlyList<TIMSAirSection> AirSections { get; }
    }
}