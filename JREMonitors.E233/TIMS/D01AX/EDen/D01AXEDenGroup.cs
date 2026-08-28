using System;
using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Providers;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenGroup : Widget<D01AXEDenGroupViewModel>
    {
        private readonly D01AXEDenDestinationText _destinationText;
        private readonly D01AXEDenStationInfo _leftStationInfo;
        private readonly D01AXEDenLocationIndicator _locationIndicator;
        private readonly D01AXEDenNextDutyInfo _nextDutyInfo;
        private readonly D01AXEDenStationInfo _rightStationInfo;
        private readonly D01AXEDenRouteSetInformation _routeSetInformation;
        private readonly D01AXEDenSlowSectionInfo _slowSectionInfo;
        private readonly Row _timeRow;
        private readonly D01AXEDenTimeCol[] _timsCols = new D01AXEDenTimeCol[D01AXEDenGroupViewModel.MaxColCount];

        public D01AXEDenGroup(RenderContext context, ScopedRenderContext scopedContext, TIMSVehicleSpec spec) :
            base(context)
        {
            var spec1 = spec;
            ViewModel = new D01AXEDenGroupViewModel(spec);
            if (spec.D01AXSpec.MayShowRouteSetInformation)
            {
                var routeSetInformation = new D01AXEDenRouteSetInformation(context);
                _routeSetInformation = routeSetInformation;
                AddChild(_routeSetInformation);
            }

            _destinationText = new D01AXEDenDestinationText(context, scopedContext, ViewModel.DestinationName);
            _destinationText.IsVisible.Bind(ViewModel.IsDestinationVisible);
            _destinationText.X.Bind(CreateComputed<float>(() =>
                ViewModel.VehicleDirection == TIMSVehicleDirection.Left
                    ? spec1.D01AXSpec.MayShowRouteSetInformation ? 140 : 4
                    : 704));
            AddChild(_destinationText);
            _leftStationInfo = new D01AXEDenStationInfo(context, scopedContext, ViewModel.LeftStation);
            _leftStationInfo.X.Bind(CreateComputed<float>(() => spec1.D01AXSpec.MayShowRouteSetInformation ? 140 : 4));
            AddChild(_leftStationInfo);
            _rightStationInfo = new D01AXEDenStationInfo(context, scopedContext, ViewModel.RightStation);
            _rightStationInfo.X.Value = 685;
            AddChild(_rightStationInfo);
            for (var i = 0; i < D01AXEDenGroupViewModel.MaxColCount; i++)
            {
                var j = i;
                _timsCols[i] = new D01AXEDenTimeCol(context, scopedContext, ViewModel.Cols[i]);
                _timsCols[i].SkipArrangementWhenHidden.Value = false;
            }

            _locationIndicator = new D01AXEDenLocationIndicator(context, ViewModel.LocationIndicator);
            AddChild(_locationIndicator);
            _timeRow = new Row(context, y: 136, widgets: _timsCols, positionSnapToPixels: true,
                rowHorizontalAlignment: 0.5f);
            _timeRow.X.Bind(CreateComputed(() =>
                MathHelper.Lerp(spec1.D01AXSpec.MayShowRouteSetInformation ? 134 : 0, 800, 0.5f)));
            AddChild(_timeRow);
            _nextDutyInfo = new D01AXEDenNextDutyInfo(context, scopedContext, spec);
            AddChild(_nextDutyInfo);
            _slowSectionInfo = new D01AXEDenSlowSectionInfo(context, scopedContext, spec);
            AddChild(_slowSectionInfo);
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected override void OnArrangeLayout(bool ignoreHidden)
        {
            ArrangeChild(_routeSetInformation, ignoreHidden);
            ArrangeChild(_destinationText, ignoreHidden);
            ArrangeChild(_leftStationInfo, ignoreHidden);
            ArrangeChild(_rightStationInfo, ignoreHidden);
            ArrangeChild(_timeRow, ignoreHidden);
            for (var i = 0; i < ViewModel.StationColCount; i++)
            {
                var colWidget = _timsCols[ViewModel.StationColStart + i];
                _locationIndicator.ColumnPixelXCoords[i] = _timeRow.X + colWidget.X;
            }

            ArrangeChild(_slowSectionInfo, ignoreHidden);
            ArrangeChild(_nextDutyInfo, ignoreHidden);
        }
    }

    public class D01AXEDenGroupViewModel : TIMSViewModel
    {
        public const int MaxColCount = 7;
        private IVehicleStateProvider _vehicleStateProvider;

        public D01AXEDenGroupViewModel(TIMSVehicleSpec vehicleSpec)
        {
            Spec = vehicleSpec;
            var spec = vehicleSpec.D01AXSpec;
            ColCount = spec.MayShowRouteSetInformation ? 5 : 7;
            IsDestinationVisible = new Signal<bool>();
            DestinationName = new Signal<string>(string.Empty);
            CurrentStationIndex = new Signal<int>(-1);
            LeftStation = new D01AXEDenStationInfoStates();
            AddSubViewModel(LeftStation);
            RightStation = new D01AXEDenStationInfoStates();
            AddSubViewModel(RightStation);
            LocationIndicator = new D01AXEDenLocationIndicatorStates();
            AddSubViewModel(LocationIndicator);
            for (var i = 0; i < Cols.Length; i++)
            {
                Cols[i] = new D01AXEDenTimeColStates();
                AddSubViewModel(Cols[i]);
            }
        }

        public TIMSVehicleSpec Spec { get; }

        public int ColOffset => (MaxColCount - ColCount) / 2;
        public Signal<bool> IsDestinationVisible { get; }
        public Signal<string> DestinationName { get; }
        public int ColCount { get; }
        public D01AXEDenStationInfoStates LeftStation { get; }
        public D01AXEDenStationInfoStates RightStation { get; }
        public D01AXEDenLocationIndicatorStates LocationIndicator { get; }
        public D01AXEDenTimeColStates[] Cols { get; } = new D01AXEDenTimeColStates[MaxColCount];

        public Signal<int> CurrentStationIndex { get; }

        private int TimingStationColIndex => (TIMSService.VehicleDirection == TIMSVehicleDirection.Left
            ? 1
            : ColCount - 2) + ColOffset;

        private int AlightingStationColIndex => (TIMSService.VehicleDirection == TIMSVehicleDirection.Left
            ? 0
            : ColCount - 1) + ColOffset;

        public int StationColCount => ColCount - 2;

        public int StationColStart => (TIMSService?.VehicleDirection == TIMSVehicleDirection.Left
            ? 2
            : 0) + ColOffset;

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            VehicleDirection.Value = TIMSService.VehicleDirection;
            LocationIndicator.IsVisible.Value = ICCardService.CurrentStation != null;
            if (ICCardService.CurrentOverrideDestination != null)
            {
                IsDestinationVisible.Value = true;
                DestinationName.Value =
                    TIMSHelper.ParseHorizontalStationName(ICCardService.CurrentOverrideDestination.Name);
            }
            else
            {
                IsDestinationVisible.Value = false;
            }

            CurrentStationIndex.Value = ICCardService.CurrentStationIndex;

            for (var i = 0; i < Cols.Length; i++) Cols[i].IsVisible.Value = false;

            LeftStation.IsVisible.Value = false;
            RightStation.IsVisible.Value = false;

            var currentStnIdx = CurrentStationIndex.Value;
            if (currentStnIdx < 0 || ICCardService.CurrentLeg?.Stations == null) return;
            var allStations = ICCardService.CurrentLeg.Stations;
            var scanEnd = allStations.Count;

            var isLeftDirection = TIMSService.VehicleDirection == TIMSVehicleDirection.Left;

            var visibleStart = currentStnIdx;
            var stationColCount = StationColCount;
            if (visibleStart + stationColCount > scanEnd)
                visibleStart = Math.Max(0, scanEnd - stationColCount);

            var nextTimingStn = ICCardService.GetNextTimingStationAfter(visibleStart + stationColCount);
            var alightingStn = ICCardService.CurrentAlightingStation;
            var prevTimingStn = ICCardService.PrevTimingStation;

            var nextTimingStnIdx = IndexOfStation(allStations, nextTimingStn);
            var nextTimingInRange = nextTimingStnIdx >= visibleStart &&
                                    nextTimingStnIdx < visibleStart + stationColCount;

            var alightingStnIdx = IndexOfStation(allStations, alightingStn);
            var alightingInRange = alightingStnIdx >= visibleStart &&
                                   alightingStnIdx < visibleStart + stationColCount;

            var shouldHideNextTiming = false;
            if (nextTimingStn != null)
            {
                if (alightingStn != null)
                    shouldHideNextTiming = nextTimingInRange && alightingInRange;
                else
                    shouldHideNextTiming = nextTimingInRange;
            }

            var shouldHideAlighting = false;
            if (alightingStn != null)
            {
                if (nextTimingStn != null)
                    shouldHideAlighting = nextTimingInRange && alightingInRange;
                else
                    shouldHideAlighting = alightingInRange;
            }

            UpdateTimingStationInfo(allStations, scanEnd, isLeftDirection, prevTimingStn, alightingStn,
                shouldHideAlighting);
            UpdateTimingStationCol(nextTimingStn, alightingStn, shouldHideNextTiming, shouldHideAlighting);
            UpdateAlightingStationCol(alightingStn, shouldHideAlighting);

            UpdateStationCols(allStations, scanEnd, isLeftDirection, alightingStn, visibleStart, shouldHideAlighting);

            UpdateLocationIndicator(allStations, isLeftDirection, stationColCount, visibleStart);
        }

        private void UpdateLocationIndicator(IReadOnlyList<TIMSStation<E233SignalSystem>> allStations,
            bool isLeftDirection, int stationColCount, int visibleStart)
        {
            LocationIndicator.VehicleDirection.Value = TIMSService.VehicleDirection;
            LocationIndicator.StationColCount.Value = stationColCount;
            var scanEnd = allStations.Count;
            var visibleCount = Math.Max(0, scanEnd - visibleStart);
            if (visibleCount > stationColCount) visibleCount = stationColCount;

            var segmentCount = Math.Max(0, visibleCount - 1);
            LocationIndicator.SegmentCount.Value = segmentCount;

            for (var i = 0; i < segmentCount; i++)
            {
                var stationIdx = visibleStart + i;
                if (stationIdx >= allStations.Count) continue;

                var station = allStations[stationIdx];
                LocationIndicator.SegmentColors[i] = station.LineColor?.ToColor4() ?? MonitorColors.White;
                LocationIndicator.SegmentWidths[i] = MathHelper.Max(station.LineStrokeWidth, 1);
            }

            var currentStnIdx = CurrentStationIndex.Value;
            var relativeStnIdx = currentStnIdx - visibleStart;
            var currentColIndex = isLeftDirection
                ? stationColCount - 1 - relativeStnIdx
                : relativeStnIdx;
            LocationIndicator.CurrentStationColumnIndex.Value =
                Math.Max(0, Math.Min(stationColCount - 1, currentColIndex));
            float trainProgress = 0;
            if (currentStnIdx >= 0 && currentStnIdx + 1 < allStations.Count)
            {
                var stationA = allStations[currentStnIdx];
                var stationB = allStations[currentStnIdx + 1];
                var distanceTotal = stationB.MinLocation - stationA.MinLocation;
                if (distanceTotal > 0)
                {
                    var distanceTraveled = _vehicleStateProvider.Location - stationA.MinLocation;
                    trainProgress = (float)Math.Max(0, Math.Min(1, distanceTraveled / distanceTotal));
                }
            }

            LocationIndicator.TrainProgress.Value = trainProgress;
        }

        private void UpdateTimingStationInfo(IReadOnlyList<TIMSStation<E233SignalSystem>> allStations, int scanEnd,
            bool isLeftDirection, TIMSStation<E233SignalSystem> prevTimingStn,
            TIMSStation<E233SignalSystem> alightingStn, bool shouldHideAlighting)
        {
            var currentStnIdx = CurrentStationIndex.Value;

            var prevTimingInfo = isLeftDirection ? RightStation : LeftStation;
            var alightingInfo = isLeftDirection ? LeftStation : RightStation;

            if (currentStnIdx >= 0 && currentStnIdx < scanEnd && prevTimingStn != null)
            {
                var firstStation = allStations[currentStnIdx];
                var isStarting = ICCardService.IsStartingStation(ICCardService.CurrentLegIndex, currentStnIdx);
                var shouldShowPrevTimingStation = !firstStation.IsTimingStation || isStarting;
                if (shouldShowPrevTimingStation) FillStationInfo(prevTimingInfo, prevTimingStn, true, false);
            }
            else
            {
                prevTimingInfo.IsVisible.Value = false;
            }

            if (alightingStn != null && !shouldHideAlighting)
            {
                var isTerminal = ICCardService.CurrentOverrideDestination == null;
                FillStationInfo(alightingInfo, alightingStn, false, isTerminal);
            }
            else
            {
                alightingInfo.IsVisible.Value = false;
            }
        }

        private static void FillStationInfo(D01AXEDenStationInfoStates info,
            TIMSStation<E233SignalSystem> station, bool isTimingStation, bool isTerminal)
        {
            info.IsVisible.Value = true;
            info.IsTimingStation.Value = isTimingStation;
            info.TopHoursAndMinutes.Value = "";
            info.TopSeconds.Value = "";
            info.BottomHoursAndMinutes.Value = "";
            info.BottomSeconds.Value = "";
            info.DepartureChar.Value = "";
            info.StationName.Value = TIMSHelper.ParseHorizontalStationName(station.Name);
            info.StationColor.Value = station.EDenTextColor?.ToColor4() ?? MonitorColors.White;
            info.StationBackgroundColor.Value = station.Color?.ToColor4() ?? Colors.Transparent;
            info.TrackName.Value = station.TrackName?.ToFullWidth() ?? "";
            if (isTimingStation)
            {
                info.TopHoursAndMinutes.Value = TIMSHelper.GetHoursAndMinutes(null, station.DepartureTime);
                info.TopSeconds.Value = TIMSHelper.GetSeconds(station.DepartureTime, false, true);
            }
            else
            {
                var arrivalTime = station.ArrivalTime;
                var showTopRow = true;

                if (!isTerminal && arrivalTime.HasValue && station.StopDuration > 0)
                {
                    var calculatedDeparture = arrivalTime.Value.Add(TimeSpan.FromSeconds(station.StopDuration));

                    if (station.DepartureTime.HasValue && calculatedDeparture > station.DepartureTime.Value)
                        calculatedDeparture = station.DepartureTime.Value;

                    if (station.DepartureTime.HasValue && calculatedDeparture == station.DepartureTime.Value)
                        showTopRow = false;
                    else
                        arrivalTime = calculatedDeparture;
                }

                if (showTopRow)
                {
                    info.TopHoursAndMinutes.Value = TIMSHelper.GetHoursAndMinutes(null, arrivalTime);
                    info.TopSeconds.Value = TIMSHelper.GetSeconds(arrivalTime, false, true);
                }
                else
                {
                    info.TopHoursAndMinutes.Value = "";
                    info.TopSeconds.Value = "";
                }

                info.BottomHoursAndMinutes.Value = TIMSHelper.GetHoursAndMinutes(null, station.DepartureTime);
                info.BottomSeconds.Value = TIMSHelper.GetSeconds(station.DepartureTime, false, true);
                if (station.DepartureTime.HasValue) info.DepartureChar.Value = "発";
            }
        }

        private void UpdateTimingStationCol(TIMSStation<E233SignalSystem> nextTimingStn,
            TIMSStation<E233SignalSystem> alightingStn,
            bool shouldHideNextTiming,
            bool shouldHideAlighting)
        {
            var colStates = Cols[TimingStationColIndex];
            colStates.IsVisible.Value = true;

            if (nextTimingStn == null || shouldHideNextTiming)
            {
                ClearCol(colStates);
                return;
            }

            var isTerminal = nextTimingStn == alightingStn && ICCardService.CurrentOverrideDestination == null;
            var isAlighting = nextTimingStn == alightingStn && !shouldHideAlighting;

            FillStationCol(colStates, nextTimingStn, isTerminal, false,
                isAlighting);
        }

        private void UpdateAlightingStationCol(TIMSStation<E233SignalSystem> alightingStn, bool shouldHideAlighting)
        {
            var colStates = Cols[AlightingStationColIndex];
            colStates.IsVisible.Value = true;
            if (alightingStn == null || shouldHideAlighting)
            {
                ClearCol(colStates);
                return;
            }

            var isTerminal = ICCardService.CurrentOverrideDestination == null;

            FillStationCol(colStates, alightingStn, isTerminal, false,
                true);
        }

        private void UpdateStationCols(IReadOnlyList<TIMSStation<E233SignalSystem>> allStations, int scanEnd,
            bool isLeftDirection, TIMSStation<E233SignalSystem> alightingStn, int visibleStart,
            bool shouldHideAlighting)
        {
            var stationColCount = StationColCount;
            var colStart = StationColStart;
            for (var i = 0; i < stationColCount; i++)
            {
                var stationIdx = isLeftDirection
                    ? visibleStart + (stationColCount - 1 - i)
                    : visibleStart + i;
                if (stationIdx >= scanEnd) continue;

                var station = allStations[stationIdx];
                var colStates = Cols[colStart + i];
                colStates.IsVisible.Value = true;

                var isTerminal = station == alightingStn && ICCardService.CurrentOverrideDestination == null;
                var isOrigin = ICCardService.IsStartingStation(ICCardService.CurrentLegIndex, stationIdx);
                var isAlighting = station == alightingStn && !shouldHideAlighting;

                FillStationCol(colStates, station, isTerminal, isOrigin,
                    isAlighting);
            }
        }

        private void FillStationCol(D01AXEDenTimeColStates colStates, TIMSStation<E233SignalSystem> station,
            bool isThisStationTerminal = false, bool isThisStationStarting = false, bool isThisStationAlighting = false)
        {
            colStates.IsVisible.Value = true;
            ClearCol(colStates);
            colStates.StationName.Value = TIMSHelper.ParseVerticalStationName(station.Name);
            colStates.StationColor.Value = station.EDenTextColor?.ToColor4() ?? MonitorColors.White;
            colStates.StationBackgroundColor.Value = station.Color?.ToColor4() ?? Colors.Transparent;
            if (isThisStationStarting || isThisStationAlighting) return;
            colStates.StationTask.Value = GetStationTask(station.StationTask);
            colStates.TrackName.Value = station.TrackName?.ToFullWidth().Trim() ?? "";
            var arrivalIndicator = GetArrivalIndicatorText(station, TIMSService.VehicleDirection);
            colStates.ArrivalIndicator.Value = arrivalIndicator;
            if (string.IsNullOrEmpty(arrivalIndicator))
            {
                if (!isThisStationTerminal && station.StopType != TIMSStopType.Pass &&
                    station.ArrivalTime.HasValue && station.StopDuration > 0)
                {
                    var calculatedDeparture = station.ArrivalTime.Value.Add(TimeSpan.FromSeconds(station.StopDuration));

                    if (station.DepartureTime.HasValue && calculatedDeparture > station.DepartureTime.Value)
                        calculatedDeparture = station.DepartureTime.Value;

                    if (station.DepartureTime.HasValue && calculatedDeparture == station.DepartureTime.Value)
                    {
                        colStates.ArrivalMinutes.Value = "";
                        colStates.ArrivalSeconds.Value = "";
                    }
                    else
                    {
                        colStates.ArrivalMinutes.Value = GetMinutes(calculatedDeparture);
                        colStates.ArrivalSeconds.Value = TIMSHelper.GetSeconds(calculatedDeparture, false, true);
                    }
                }
                else
                {
                    colStates.ArrivalMinutes.Value = GetMinutes(station.ArrivalTime);
                    colStates.ArrivalSeconds.Value = TIMSHelper.GetSeconds(station.ArrivalTime, false, true);
                }
            }

            var departureIndicator = GetDepartureIndicatorText(station);
            colStates.DepartureIndicator.Value = departureIndicator;
            if (isThisStationTerminal || !string.IsNullOrEmpty(departureIndicator))
                return;
            colStates.DepartureMinutes.Value = GetMinutes(station.DepartureTime);
            colStates.DepartureSeconds.Value = TIMSHelper.GetSeconds(station.DepartureTime, false, true);
        }

        private static void ClearCol(D01AXEDenTimeColStates colStates)
        {
            colStates.StationName.Value = "";
            colStates.StationTask.Value = "";
            colStates.ArrivalMinutes.Value = "";
            colStates.ArrivalSeconds.Value = "";
            colStates.ArrivalIndicator.Value = "";
            colStates.DepartureMinutes.Value = "";
            colStates.DepartureSeconds.Value = "";
            colStates.DepartureIndicator.Value = "";
            colStates.TrackName.Value = "";
            colStates.StationColor.Value = MonitorColors.White;
            colStates.StationBackgroundColor.Value = Colors.Transparent;
        }

        private static int IndexOfStation(IReadOnlyList<TIMSStation<E233SignalSystem>> stations,
            TIMSStation<E233SignalSystem> target)
        {
            if (target == null) return -1;
            for (var i = 0; i < stations.Count; i++)
                if (stations[i] == target)
                    return i;

            return -1;
        }

        private static string GetStationTask(TIMSStationTask task)
        {
            switch (task)
            {
                case TIMSStationTask.None:
                    return "";
                case TIMSStationTask.Waiting:
                    return "待";
                case TIMSStationTask.Adjustment:
                    return "整";
                case TIMSStationTask.Decoupling:
                    return "分";
                case TIMSStationTask.Coupling:
                default:
                    return "併";
            }
        }

        private static string GetMinutes(TimeSpan? time)
        {
            if (!time.HasValue) return "\u2003\u2003";
            return time.Value.Minutes.ToString().PadLeft(2, '0').ToFullWidth();
        }

        private static string GetArrivalIndicatorText(TIMSStation<E233SignalSystem> station,
            TIMSVehicleDirection vehicleDirection)
        {
            if (station.StopType == TIMSStopType.Pass)
                return vehicleDirection == TIMSVehicleDirection.Left ? "←" : "→";
            if (!IsArrivalTimeDisplayed(station))
                if (station.StopType == TIMSStopType.Stop && station.ShowStopText)
                    return "停";
            return "";
        }

        private static bool IsArrivalTimeDisplayed(TIMSStation<E233SignalSystem> station)
        {
            if (!station.ArrivalTime.HasValue) return false;
            if (station.StopDuration <= 0) return true;
            var calculatedDeparture = station.ArrivalTime.Value.Add(TimeSpan.FromSeconds(station.StopDuration));
            if (station.DepartureTime.HasValue && calculatedDeparture > station.DepartureTime.Value)
                calculatedDeparture = station.DepartureTime.Value;
            return !station.DepartureTime.HasValue || calculatedDeparture != station.DepartureTime.Value;
        }

        private string GetDepartureIndicatorText(TIMSStation<E233SignalSystem> station)
        {
            return "";
        }
    }
}