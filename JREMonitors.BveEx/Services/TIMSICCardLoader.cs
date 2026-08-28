using System;
using System.Collections.Generic;
using System.Linq;
using BveTypes.ClassWrappers;
using BveTypes.ClassWrappers.Extensions;
using JREMonitors.BveEx.Configs.Vehicle.TIMS;
using JREMonitors.BveEx.Utils;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE;

namespace JREMonitors.BveEx.Services
{
    public static class TIMSICCardLoader
    {
        public static TIMSICCardLoadResult<TSignal> Load<TSignal>(
            string icCardPath,
            IReadOnlyDictionary<string, TIMSFormationSpec> formationSpecs,
            string defaultFormation,
            TSignal? defaultSignalSystem,
            IReadOnlyCollection<TSignal> allowedSignalSystems,
            string vehicleName,
            WrappedSortedList<string, Station> stations,
            out IReadOnlyList<string> readFilePaths
        ) where TSignal : struct, Enum
        {
            var timsICCardConfig = ConfigLoader.LoadConfig<TIMSICCardConfig>(new[] { icCardPath },
                out readFilePaths, new TIMSPolymorphicResolver(typeof(TSignal)));
            if (stations.Count == 0)
                throw new InvalidOperationException("Bve Station List is empty.");
            var sortedBveStations = stations.ToListSafe()
                .OrderBy(pair => pair.Value.MinStopPosition)
                .ToList();
            var stationIdToSortedIndex = new Dictionary<string, int>();
            for (var i = 0; i < sortedBveStations.Count; i++)
                stationIdToSortedIndex[sortedBveStations[i].Key.ToLower()] = i;

            var legs = new List<TIMSLeg<TSignal>>();
            if (timsICCardConfig.Legs != null && timsICCardConfig.Legs.Length > 0)
            {
                var currentFormation = defaultFormation;
                var currentActiveSignalSystem = defaultSignalSystem;
                var currentRadioChannel = "";
                var currentStandardOperatingSpeed = null as string;
                TIMSTrainType? currentTrainType = null;
                for (var i = 0; i < timsICCardConfig.Legs.Length; i++)
                {
                    var legConfig = timsICCardConfig.Legs[i];
                    if (legConfig.Formation != null && !formationSpecs.ContainsKey(legConfig.Formation))
                        throw new InvalidOperationException(
                            $"Invalid formation '{legConfig.Formation}' for {vehicleName}.");
                    if (legConfig.Formation != null) currentFormation = legConfig.Formation;
                    var timsStations = new List<TIMSStation<TSignal>>();
                    var signalSystemChangePoints = new List<TIMSSignalSystemChangePoint<TSignal>>();
                    var mileageCorrectionPoints = new List<TIMSMileageCorrectionPoint>();
                    var slowSections = new List<(int startLocation, int endLocation, int speedLimit)>();
                    var airSections = new List<TIMSAirSection>();
                    var seenStationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var routeNodeConfig in legConfig.RouteNodes)
                        if (routeNodeConfig is TIMSStationConfig<TSignal> timsStationConfig)
                        {
                            var stationId = timsStationConfig.StationId.ToLower();

                            if (stationId == null) throw new InvalidOperationException("stationId cannot be null.");
                            if (!seenStationIds.Add(stationId))
                                throw new InvalidOperationException(
                                    $"Duplicate station '{stationId}' in Leg {i}.");
                            if (!stations.TryGetValue(stationId, out var bveStation))
                                throw new InvalidOperationException($"Invalid stationId '{stationId}'.");
                            if (timsStations.Count > 0 && bveStation.MinStopPosition <= timsStations.Last().MinLocation)
                                throw new InvalidOperationException(
                                    $"Stations in Leg {i} must be sorted in ascending order of their starting position. " +
                                    $"Station '{stationId}' at position {bveStation.MinStopPosition} is out of order compared to the previous station '{timsStations.Last().StationId}' at position {timsStations.Last().MinLocation}.");
                            var stopType = timsStationConfig.StopType ??
                                           (bveStation.Pass ? TIMSStopType.Pass : TIMSStopType.Stop);
                            var departureTime = timsStationConfig.OverrideDepartureTime;
                            if (bveStation.IsTerminal)
                                departureTime = null;
                            else if (departureTime == null && bveStation.DepartureTimeMilliseconds >= 0)
                                departureTime = bveStation.DepartureTime;
                            var arrivalTime = timsStationConfig.OverrideArrivalTime;
                            if (arrivalTime == null && bveStation.ArrivalTimeMilliseconds >= 0)
                                arrivalTime = bveStation.ArrivalTime;
                            if (arrivalTime == null && stopType == TIMSStopType.Pass) arrivalTime = departureTime;

                            var stopDuration = timsStationConfig.OverrideStopDuration;
                            if (stopDuration == null)
                            {
                                stopDuration = (int)bveStation.StoppageTime.TotalSeconds;
                                if (stopDuration <= 0 && arrivalTime != null && departureTime != null)
                                    stopDuration = (int)(departureTime.Value - arrivalTime.Value).TotalSeconds;
                            }

                            double arrivalHintOffset;
                            var rawOffset = timsStationConfig.ArrivalHintOffset;
                            if (timsStations.Count > 0)
                            {
                                var prevStation = timsStations.Last();
                                var delta = bveStation.MinStopPosition - prevStation.MinLocation;
                                arrivalHintOffset = rawOffset <= 0.0 ? Math.Min(1000.0, delta / 2.0) : rawOffset;

                                if (arrivalHintOffset > delta) arrivalHintOffset = delta;
                            }
                            else
                            {
                                arrivalHintOffset = rawOffset > 0.0 ? rawOffset : 0.0;
                            }

                            if (timsStationConfig.IsActiveSignalSystemExplicitlySet ||
                                timsStationConfig.ActiveSignalSystem.HasValue)
                            {
                                if (allowedSignalSystems == null || allowedSignalSystems.Count == 0)
                                    throw new InvalidOperationException(
                                        $"activeSignalSystem is not supported for {vehicleName}");
                                if (!allowedSignalSystems.Contains(timsStationConfig.ActiveSignalSystem.Value))
                                    throw new InvalidOperationException(
                                        $"Invalid activeSignalSystem '{timsStationConfig.ActiveSignalSystem.Value}' for {vehicleName}.\n" +
                                        $"Allowed values are: {string.Join(", ", allowedSignalSystems)}.");
                                currentActiveSignalSystem = timsStationConfig.ActiveSignalSystem;
                            }

                            if (timsStationConfig.IsTrainTypeExplicitlySet || timsStationConfig.TrainType.HasValue)
                                currentTrainType = timsStationConfig.TrainType;
                            if (timsStationConfig.IsRadioChannelExplicitlySet ||
                                !string.IsNullOrWhiteSpace(timsStationConfig.RadioChannel))
                                currentRadioChannel = timsStationConfig.RadioChannel;
                            if (timsStationConfig.IsStandardOperatingSpeedExplicitlySet ||
                                !string.IsNullOrWhiteSpace(timsStationConfig.StandardOperatingSpeed))
                                currentStandardOperatingSpeed = timsStationConfig.StandardOperatingSpeed;

                            TIMSStationSwitchMode switchMode;
                            if (bveStation.Pass)
                            {
                                if (timsStationConfig.SwitchMode == TIMSStationSwitchMode.DoorOpen)
                                    throw new InvalidOperationException(
                                        $"Pass station '{stationId}' cannot have 'doorOpen' switchMode.");

                                switchMode = TIMSStationSwitchMode.MinStopPosition;
                            }
                            else
                            {
                                switchMode = timsStationConfig.SwitchMode ??
                                             (legConfig.DisplayMode == TIMSDisplayMode.MDen
                                                 ? TIMSStationSwitchMode.DoorOpen
                                                 : TIMSStationSwitchMode.MinStopPosition);
                            }

                            var timsStation = new TIMSStation<TSignal>(
                                stationId,
                                timsStationConfig.DisplayName ?? bveStation.Name,
                                timsStationConfig.RemappedMileage,
                                timsStationConfig.MileageDirection,
                                bveStation.MinStopPosition,
                                bveStation.MaxStopPosition,
                                switchMode,
                                currentActiveSignalSystem
                            )
                            {
                                TrackName = timsStationConfig.TrackName,
                                StopType = stopType,
                                ShowStopText = timsStationConfig.ShowStopText,
                                ArrivalTime = arrivalTime,
                                DepartureTime = departureTime,
                                Color = timsStationConfig.Color,
                                TrackColor = timsStationConfig.TrackColor,
                                EDenTextColor = timsStationConfig.EDenTextColor,
                                LineColor = timsStationConfig.LineColor,
                                LineStrokeWidth = timsStationConfig.LineStrokeWidth,
                                RadioChannel = currentRadioChannel,
                                StandardOperatingSpeed = currentStandardOperatingSpeed,
                                ArrivalHintOffset = arrivalHintOffset,
                                StationTask = timsStationConfig.StationTask,
                                SpeedLimitArrival = timsStationConfig.SpeedLimitArrival,
                                SpeedLimitDeparture = timsStationConfig.SpeedLimitDeparture,
                                IsTimingStation = timsStationConfig.IsTimingStation ??
                                                  (arrivalTime.HasValue || departureTime.HasValue),
                                StopDuration = stopDuration.Value,
                                TrainType = currentTrainType,
                                SignalSystemSwitchDuration = Math.Max(0, timsStationConfig.SignalSystemSwitchDuration),
                                TrainTypeSwitchDuration = Math.Max(0, timsStationConfig.TrainTypeSwitchDuration),
                                StationBlockStartOffset = timsStationConfig.StationBlockStartOffset,
                                StationBlockEndOffset = timsStationConfig.StationBlockEndOffset
                            };

                            timsStations.Add(timsStation);
                        }
                        else if (routeNodeConfig is TIMSSignalSystemChangePointConfig<TSignal> changePointConfig)
                        {
                            if (!allowedSignalSystems.Contains(changePointConfig.SignalSystem))
                                throw new InvalidOperationException(
                                    $"Invalid signalSystem '{changePointConfig.SignalSystem}' for {vehicleName}.\n" +
                                    $"Allowed values are: {string.Join(", ", allowedSignalSystems)}.");
                            signalSystemChangePoints.Add(new TIMSSignalSystemChangePoint<TSignal>(
                                changePointConfig.StartLocation,
                                changePointConfig.SignalSystem
                            ));
                        }
                        else if (routeNodeConfig is TIMSMileageCorrectionPointConfig correctionPointConfig)
                        {
                            mileageCorrectionPoints.Add(new TIMSMileageCorrectionPoint(
                                correctionPointConfig.Location,
                                correctionPointConfig.RemappedMileage,
                                correctionPointConfig.MileageDirection
                            ));
                        }
                        else if (routeNodeConfig is TIMSSlowSectionConfig slowSectionConfig)
                        {
                            if (slowSectionConfig.StartLocation < 0 || slowSectionConfig.EndLocation < 0)
                                throw new InvalidOperationException(
                                    $"Slow section locations must be non-negative (Start={slowSectionConfig.StartLocation}, End={slowSectionConfig.EndLocation}).");
                            if (slowSectionConfig.StartLocation > slowSectionConfig.EndLocation)
                                throw new InvalidOperationException(
                                    $"Slow section StartLocation must not exceed EndLocation (Start={slowSectionConfig.StartLocation}, End={slowSectionConfig.EndLocation}).");
                            slowSections.Add((slowSectionConfig.StartLocation, slowSectionConfig.EndLocation,
                                slowSectionConfig.SpeedLimit));
                        }
                        else if (routeNodeConfig is TIMSAirSectionConfig airSectionConfig)
                        {
                            if (airSectionConfig.StartLocation < 0 || airSectionConfig.EndLocation < 0)
                                throw new InvalidOperationException(
                                    $"Air section locations must be non-negative (Start={airSectionConfig.StartLocation}, End={airSectionConfig.EndLocation}).");
                            if (airSectionConfig.EndLocation < airSectionConfig.StartLocation)
                                throw new InvalidOperationException(
                                    $"Air section EndLocation must not be less than StartLocation (Start={airSectionConfig.StartLocation}, End={airSectionConfig.EndLocation}).");
                            if (airSectionConfig.HintOffset < 0)
                                throw new InvalidOperationException(
                                    $"Air section HintOffset must be non-negative (HintOffset={airSectionConfig.HintOffset}).");
                            airSections.Add(new TIMSAirSection(airSectionConfig.StartLocation,
                                airSectionConfig.EndLocation, airSectionConfig.HintOffset));
                        }

                    if (timsStations.Count < 2)
                        throw new InvalidOperationException("Leg must have at least 2 stations.");
                    airSections.Sort((a, b) => a.StartLocation.CompareTo(b.StartLocation));
                    var destination = legConfig.OverrideDestination == null
                        ? null
                        : new TIMSDestination(legConfig.OverrideDestination.Name);

                    var nextDuty = legConfig.NextDuty == null
                        ? null
                        : new TIMSNextDuty(string.IsNullOrWhiteSpace(legConfig.NextDuty.TrainNumber)
                                ? string.Empty
                                : legConfig.NextDuty.TrainNumber, legConfig.NextDuty.ArrivalTime,
                            legConfig.NextDuty.DepartureTime);
                    char? trainNumberChar = null;
                    if (string.IsNullOrWhiteSpace(legConfig.TrainNumberChar))
                        trainNumberChar = null;
                    else
                        trainNumberChar = legConfig.TrainNumberChar[0];

                    legs.Add(new TIMSLeg<TSignal>(
                        string.IsNullOrWhiteSpace(legConfig.TrainNumber) ? string.Empty : legConfig.TrainNumber,
                        trainNumberChar,
                        currentFormation,
                        legConfig.DisplayMode,
                        destination,
                        nextDuty,
                        legConfig.SwitchDuration,
                        timsStations,
                        signalSystemChangePoints,
                        mileageCorrectionPoints,
                        slowSections,
                        airSections
                    ));
                }
            }

            for (var i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                if (leg.Stations.Count == 0) continue;

                var sortedLegStations = leg.Stations
                    .Select(s => stations[s.StationId])
                    .OrderBy(s => s.MinStopPosition)
                    .ToList();
                var bveLastStation = sortedLegStations.Last();
                if (bveLastStation.Pass)
                    throw new InvalidOperationException(
                        $"The terminal station '{bveLastStation.Name}' of leg {i} cannot be passed.");
            }

            var legIntervals = new List<(int MinIdx, int MaxIdx)>();
            for (var i = 0; i < legs.Count; i++)
            {
                var indices = legs[i].Stations.Select(s => stationIdToSortedIndex[s.StationId]).ToList();
                if (indices.Count == 0) throw new InvalidOperationException($"Leg {i} has no valid station nodes.");

                legIntervals.Add((indices.Min(), indices.Max()));
            }

            for (var i = 0; i < legIntervals.Count; i++)
            for (var j = i + 1; j < legIntervals.Count; j++)
            {
                var intervalI = legIntervals[i];
                var intervalJ = legIntervals[j];

                if (j == i + 1)
                {
                    if (intervalI.MaxIdx > intervalJ.MinIdx)
                        throw new InvalidOperationException(
                            $"Invalid overlap detected between Leg {i} and adjacent Leg {j}. " +
                            $"Leg {i} spans up to station index {intervalI.MaxIdx}, but Leg {j} starts earlier at index {intervalJ.MinIdx}. " +
                            "Overlapping is strictly restricted to the shared boundary station.");
                }
                else
                {
                    if (intervalI.MaxIdx >= intervalJ.MinIdx)
                        throw new InvalidOperationException(
                            $"Non-adjacent Leg {i} and Leg {j} have overlapping stations. This is strictly prohibited.");
                }
            }

            var uniqueStations = legs.SelectMany(l => l.Stations)
                .GroupBy(s => s.StationId.ToLower())
                .Select(g => g.First())
                .ToList();

            for (var i = 0; i < uniqueStations.Count; i++)
            {
                var s1 = uniqueStations[i];
                var start1 = s1.Location / 2 - s1.StationBlockStartOffset;
                var end1 = s1.Location / 2 + s1.StationBlockEndOffset;

                for (var j = i + 1; j < uniqueStations.Count; j++)
                {
                    var s2 = uniqueStations[j];
                    var start2 = s2.Location / 2 - s2.StationBlockStartOffset;
                    var end2 = s2.Location / 2 + s2.StationBlockEndOffset;
                    if (start1 < end2 && start2 < end1)
                        throw new InvalidOperationException(
                            $"Overlap detected between station '{s1.Name}' (ID: {s1.StationId}, Block: [{start1:F2}, {end1:F2}]) " +
                            $"and station '{s2.Name}' (ID: {s2.StationId}, Block: [{start2:F2}, {end2:F2}]). " +
                            "Station block intervals must not overlap.");
                }
            }

            return new TIMSICCardLoadResult<TSignal>
            {
                Depot = timsICCardConfig.Depot,
                DutyNumber = timsICCardConfig.DutyNumber,
                Legs = legs
            };
        }
    }

    public class TIMSICCardLoadResult<TSignal> where TSignal : struct, Enum
    {
        public string Depot { get; set; }
        public string DutyNumber { get; set; }
        public IReadOnlyList<TIMSLeg<TSignal>> Legs { get; set; }
    }
}