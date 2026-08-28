using System;
using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.MDen
{
    public class D01AXMDenGroup : Widget<D01AXMDenGroupViewModel>
    {
        private readonly D01AXMDenTimeRow[] _timeRows = new D01AXMDenTimeRow[D01AXMDenGroupViewModel.RowCount];

        public D01AXMDenGroup(RenderContext context, ScopedRenderContext scopedContext) : base(context)
        {
            ViewModel = new D01AXMDenGroupViewModel();
            for (var i = 0; i < D01AXMDenGroupViewModel.RowCount; i++)
            {
                _timeRows[i] = new D01AXMDenTimeRow(context, scopedContext, 143 + 42 * i, ViewModel.Rows[i]);
                AddChild(_timeRows[i]);
            }

            var nextStationText = new BoundsDrawerWidget(scopedContext, this.CreateTIMSTextDrawer(
                    context: scopedContext,
                    documentSource: CreateComputed(() =>
                        RichTextParser.Raw(ViewModel.FormattedNextStationName.Value)), scaleX: 2),
                contentColor: Colors.Yellow, x: 198, y: 343);
            nextStationText.IsVisible.Bind(CreateComputed(() =>
                ViewModel.NextStopStation.Value != null && ViewModel.IsArrivalTextBlinkVisible.Value));
            AddChild(nextStationText);
            var nextStationArrivalTimeText = new D01AXMDenTimeText(context, scopedContext, 420, 343, 0,
                ViewModel.NextStationArrivalHoursAndMinutes,
                ViewModel.NextStationArrivalSeconds,
                ViewModel.NextStationArrivalIndicator,
                new Signal<bool>(),
                new Signal<Color4>(MonitorColors.White),
                new Signal<Color4>(MonitorColors.White)
            );
            nextStationArrivalTimeText.IsVisible.Bind(CreateComputed(() =>
                ViewModel.NextStopStation.Value != null));
            AddChild(nextStationArrivalTimeText);
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D01AXMDenGroupViewModel : ViewModel
    {
        public const int RowCount = 3;
        private TickTracker _blinkTracker;
        private TIMSICCardService<E233SignalSystem> _icCardService;

        public D01AXMDenGroupViewModel()
        {
            IsArrivalTextBlinkVisible = CreatePropertySlot(false);
            CurrentStationIndex = CreatePropertySlot(-1);
            CurrentStation = CreatePropertySlot<TIMSStation<E233SignalSystem>>();
            NextStopStation = CreatePropertySlot<TIMSStation<E233SignalSystem>>();
            AlightingStation = CreatePropertySlot<TIMSStation<E233SignalSystem>>();
            ArrivalHintTriggeredTime = CreatePropertySlot<TimeSpan?>();
            FormattedNextStationName = CreatePropertySlot("");
            NextStationArrivalHoursAndMinutes = CreatePropertySlot("");
            NextStationArrivalSeconds = CreatePropertySlot("");
            NextStationArrivalIndicator = CreatePropertySlot("");

            for (var i = 0; i < Rows.Length; i++)
            {
                Rows[i] = new D01AXMDenTimeRowStates();
                AddSubViewModel(Rows[i]);
            }
        }

        public D01AXMDenTimeRowStates[] Rows { get; } = new D01AXMDenTimeRowStates[RowCount];
        public IReadOnlyList<TIMSStation<E233SignalSystem>> AllStations { get; private set; }
        public PropertySlot<bool> IsArrivalTextBlinkVisible { get; }
        public PropertySlot<int> CurrentStationIndex { get; }
        public PropertySlot<TIMSStation<E233SignalSystem>> CurrentStation { get; }
        public PropertySlot<TIMSStation<E233SignalSystem>> NextStopStation { get; }
        public PropertySlot<TIMSStation<E233SignalSystem>> AlightingStation { get; }
        public PropertySlot<TimeSpan?> ArrivalHintTriggeredTime { get; }
        public PropertySlot<string> FormattedNextStationName { get; }
        public PropertySlot<string> NextStationArrivalHoursAndMinutes { get; }
        public PropertySlot<string> NextStationArrivalSeconds { get; }
        public PropertySlot<string> NextStationArrivalIndicator { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            var delayService = dataHub.Get<DelayService>();
            _blinkTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Blink));
            _icCardService = dataHub.Get<TIMSICCardService<E233SignalSystem>>();
            for (var i = 0; i < Rows.Length; i++) Rows[i].Initialize(dataHub);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            CurrentStationIndex.Value = _icCardService.CurrentStationIndex;
            var allStations = _icCardService.CurrentLeg?.Stations ?? Array.Empty<TIMSStation<E233SignalSystem>>();
            AllStations = allStations;
            CurrentStation.Value = _icCardService.CurrentStation;
            AlightingStation.Value = _icCardService.CurrentAlightingStation;
            NextStopStation.Value = _icCardService.NextStopStation;
            ArrivalHintTriggeredTime.Value = _icCardService.ArrivalHintTriggeredTime;
            UpdateBlinkState();

            for (var i = 0; i < Rows.Length; i++) ClearRow(Rows[i]);

            var currentStnIdx = CurrentStationIndex.Value;
            var nextStnEntity = NextStopStation.Value;
            var scanEnd = allStations.Count;
            var nextStnIdx = IndexOfStation(allStations, nextStnEntity);
            var maxScanLimit = Math.Max(scanEnd, nextStnIdx + 1);

            TimeSpan? lastSeenTime = null;
            TimeSpan? nextStationComparisonTime = null;

            for (var idx = 0; idx < maxScanLimit; idx++)
            {
                var currentStation = allStations[idx];
                if (idx == nextStnIdx) nextStationComparisonTime = lastSeenTime;

                var i = idx - currentStnIdx;
                if (currentStnIdx >= 0 && i >= 0 && i < Rows.Length)
                {
                    var nextStn = idx + 1 < scanEnd ? allStations[idx + 1] : null;
                    var isLastRow = i == Rows.Length - 1;
                    FillRow(Rows[i], currentStation, nextStn, lastSeenTime, isLastRow);
                }

                if (currentStation.DepartureTime.HasValue)
                    lastSeenTime = currentStation.DepartureTime;
                else if (currentStation.StopType != TIMSStopType.Pass && currentStation.ArrivalTime.HasValue)
                    lastSeenTime = currentStation.ArrivalTime;
            }

            UpdateNextStationInfo(nextStnEntity, nextStationComparisonTime);
        }

        private void UpdateBlinkState()
        {
            var triggered = ArrivalHintTriggeredTime.Value.HasValue;
            IsArrivalTextBlinkVisible.Value = !triggered || TIMSHelper.IsBlink(_blinkTracker.Sync());
        }

        private void UpdateNextStationInfo(TIMSStation<E233SignalSystem> nextStnEntity,
            TimeSpan? nextStationComparisonTime)
        {
            FormattedNextStationName.Value = "";
            NextStationArrivalIndicator.Value = "";
            NextStationArrivalHoursAndMinutes.Value = "";
            NextStationArrivalSeconds.Value = "";

            if (nextStnEntity != null)
            {
                FormattedNextStationName.Value = TIMSHelper.ParseHorizontalStationName(nextStnEntity.Name);
                var nextArrivalIndicator = GetArrivalIndicatorText(nextStnEntity);
                NextStationArrivalIndicator.Value = nextArrivalIndicator;

                if (string.IsNullOrEmpty(nextArrivalIndicator))
                {
                    NextStationArrivalHoursAndMinutes.Value =
                        TIMSHelper.GetHoursAndMinutes(nextStationComparisonTime, nextStnEntity.ArrivalTime);
                    NextStationArrivalSeconds.Value =
                        TIMSHelper.GetSeconds(nextStnEntity.ArrivalTime, true, true);
                }
            }
        }

        private void FillRow(D01AXMDenTimeRowStates rowVM, TIMSStation<E233SignalSystem> currentStation,
            TIMSStation<E233SignalSystem> nextStn, TimeSpan? lastSeenTime,
            bool isLastRow)
        {
            rowVM.IsVisible.Value = true;
            rowVM.StationName.Value = TIMSHelper.ParseHorizontalStationName(currentStation.Name);
            if (isLastRow)
            {
                rowVM.DurationMinutes.Value = "";
                rowVM.DurationSeconds.Value = "";
            }
            else
            {
                var duration = (nextStn?.ArrivalTime - currentStation.DepartureTime).MaxZero();
                rowVM.DurationMinutes.Value = (duration.HasValue && duration.Value.Minutes > 0
                    ? duration.Value.Minutes.ToString()
                    : "").PadLeft(2);
                rowVM.DurationSeconds.Value = TIMSHelper.GetSeconds(duration, false, true);
            }

            var arrivalIndicator = GetArrivalIndicatorText(currentStation);
            rowVM.ArrivalIndicator.Value = arrivalIndicator;
            rowVM.ShowArrivalPassArrow.Value = currentStation.StopType == TIMSStopType.Pass;
            if (currentStation.StopType != TIMSStopType.Pass && string.IsNullOrEmpty(arrivalIndicator))
            {
                rowVM.ArrivalHoursAndMinutes.Value =
                    TIMSHelper.GetHoursAndMinutes(lastSeenTime, currentStation.ArrivalTime);
                rowVM.ArrivalSeconds.Value = TIMSHelper.GetSeconds(currentStation.ArrivalTime, true, true);
            }

            var departureIndicator = GetDepartureIndicatorText(currentStation);
            rowVM.DepartureIndicator.Value = departureIndicator;
            rowVM.ShowDeparturePassArrow.Value = false;
            if (string.IsNullOrEmpty(departureIndicator))
            {
                var departureComparisonTime =
                    (currentStation.StopType != TIMSStopType.Pass ? currentStation.ArrivalTime : null) ??
                    lastSeenTime;
                rowVM.DepartureHoursAndMinutes.Value =
                    TIMSHelper.GetHoursAndMinutes(departureComparisonTime, currentStation.DepartureTime);
                rowVM.DepartureSeconds.Value = TIMSHelper.GetSeconds(currentStation.DepartureTime, true, true);
                if (currentStation.StopType == TIMSStopType.Pass && !currentStation.DepartureTime.HasValue)
                    rowVM.ShowDeparturePassArrow.Value = true;
            }

            if (!string.IsNullOrWhiteSpace(currentStation.TrackName))
                rowVM.TrackName.Value = currentStation.TrackName.ToFullWidth();

            if (!string.IsNullOrWhiteSpace(currentStation.SpeedLimitArrival))
                rowVM.SpeedLimitArrival.Value = currentStation.SpeedLimitArrival;

            if (!string.IsNullOrWhiteSpace(currentStation.SpeedLimitDeparture))
                rowVM.SpeedLimitDeparture.Value = currentStation.SpeedLimitDeparture;

            var currentColor = currentStation.Color?.ToColor4() ?? MonitorColors.White;
            var nextColor = nextStn?.Color?.ToColor4() ?? MonitorColors.White;
            rowVM.DurationColor.Value = nextColor;
            rowVM.StationColor.Value = currentColor;
            rowVM.ArrivalColor.Value = currentColor;
            rowVM.DepartureColor.Value = currentColor;
            rowVM.TrackColor.Value = currentColor;
        }

        private static void ClearRow(D01AXMDenTimeRowStates row)
        {
            row.IsVisible.Value = false;
            row.StationName.Value = "";
            row.DurationMinutes.Value = "";
            row.DurationSeconds.Value = "";
            row.ArrivalIndicator.Value = "";
            row.ShowArrivalPassArrow.Value = false;
            row.ShowDeparturePassArrow.Value = false;
            row.ArrivalHoursAndMinutes.Value = "";
            row.ArrivalSeconds.Value = "";
            row.DepartureIndicator.Value = "";
            row.DepartureHoursAndMinutes.Value = "";
            row.DepartureSeconds.Value = "";
            row.TrackName.Value = "";
            row.SpeedLimitArrival.Value = "";
            row.SpeedLimitDeparture.Value = "";
            row.DurationColor.Value = MonitorColors.White;
            row.StationColor.Value = MonitorColors.White;
            row.ArrivalColor.Value = MonitorColors.White;
            row.DepartureColor.Value = MonitorColors.White;
            row.TrackColor.Value = MonitorColors.White;
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

        private static string GetArrivalIndicatorText(TIMSStation<E233SignalSystem> station)
        {
            if (station.ArrivalTime.HasValue) return "";
            if (station.StopType == TIMSStopType.Stop && station.ShowStopText)
                return "停";
            return "";
        }

        public string GetDepartureIndicatorText(TIMSStation<E233SignalSystem> station)
        {
            return station == AlightingStation.Value ? "＝" : "";
        }

        protected override void OnReset()
        {
            _blinkTracker.Reset();
        }
    }
}