using System;
using System.Collections.Generic;
using System.Linq;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE.Providers;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.E233.TIMS.ICCard
{
    public abstract class TIMSICCardService<TSignal> : ITickUpdatable
        where TSignal : struct, Enum
    {
        /// <summary>
        ///     每节车厢长度（米），用于计算车尾位置
        /// </summary>
        private const double SlowSectionCarLengthMeters = 20.0;

        private readonly CarStateService _carStateService;
        private readonly IDoorStateService _doorStateService;
        private readonly PassengerStateService _passengerStateService;
        private readonly ISignalController<TSignal> _signalController;

        // 徐行区间每帧计算用到的可复用缓冲，避免每帧分配
        private readonly List<int> _slowSectionOrdered = new List<int>();

        private readonly List<(double Location, double MileageBase, double Sign, bool IsCorrection)>
            _slowSectionReferences = new List<(double Location, double MileageBase, double Sign, bool IsCorrection)>();

        private readonly List<double> _slowSectionSplits = new List<double>();

        private readonly List<(double start, double end, int speedLimit)> _slowSectionSub =
            new List<(double start, double end, int speedLimit)>();

        private readonly ITimeProvider _timeProvider;
        private readonly TIMSService _timsService;
        private readonly IVehicleStateProvider _vehicleStateProvider;
        protected readonly IDebugger Debugger;
        protected readonly IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs;

        private (TIMSSlowSection?, TIMSSlowSection?) _currentSlowSections;

        private string _dutyNumber;

        /// 列车在当前重叠站台停靠期间是否已经完成了向新运用段的切换
        private bool _hasCompletedSwitchForCurrentStop;

        /// 列车在当前车站物理范围内是否开过门
        private bool _hasOpenedDoorAtCurrentStation;

        private int _prevDoorLeg = -1;
        private int _prevDoorStation = -1;
        private string _prevFormation;
        private bool _prevInserted;
        private bool _signalSystemSwitchedForCurrentStation;
        private double _signalSystemTimer;
        private double _switchTimer;
        private bool _trainTypeSwitchedForCurrentStation;
        private double _trainTypeTimer;
        protected TIMSDisplayMode DefaultDisplayMode;
        protected string DefaultFormation;
        protected TSignal? DefaultSignalSystem;

        public TIMSICCardService(
            DataHub dataHub,
            IReadOnlyDictionary<string, TIMSFormationSpec> formationSpecs,
            string depot,
            string dutyNumber,
            TIMSDisplayMode defaultDisplayMode,
            string defaultFormation,
            TSignal? defaultSignalSystem,
            IReadOnlyList<TIMSLeg<TSignal>> legs
        )
        {
            DefaultDisplayMode = defaultDisplayMode;
            DefaultFormation = defaultFormation;
            DefaultSignalSystem = defaultSignalSystem;
            FormationSpecs = formationSpecs;
            Debugger = dataHub.GetOrNull<IDebugger>();
            _timeProvider = dataHub.Get<ITimeProvider>();
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            _signalController = dataHub.Get<ISignalController<TSignal>>();
            _passengerStateService = dataHub.Get<PassengerStateService>();
            _carStateService = dataHub.Get<CarStateService>();
            _doorStateService = dataHub.Get<IDoorStateService>();
            _timsService = dataHub.Get<TIMSService>();
            Depot = depot;
            _dutyNumber = dutyNumber;
            Legs = legs;
        }

        public object[] BeforeDeps => new object[]
            { _signalController, _carStateService, _doorStateService, _passengerStateService, _timsService };

        public object[] AfterDeps => new object[] { _vehicleStateProvider };
        public int CurrentLegIndex => Inserted ? CurrentLegIndexIgnoreInserted : -1;
        public int CurrentStationIndex => Inserted ? CurrentStationIndexIgnoreInserted : -1;
        public int CurrentLegIndexIgnoreInserted { get; private set; } = -1;
        public int CurrentStationIndexIgnoreInserted { get; private set; } = -1;

        public TIMSLeg<TSignal> CurrentLegIgnoreInserted
        {
            get
            {
                if (CurrentLegIndexIgnoreInserted < 0 || CurrentLegIndexIgnoreInserted >= Legs.Count)
                    return null;
                return Legs[CurrentLegIndexIgnoreInserted];
            }
        }

        public TIMSStation<TSignal> CurrentStationIgnoreInserted
        {
            get
            {
                var currentLeg = CurrentLegIgnoreInserted;
                if (currentLeg == null || CurrentStationIndexIgnoreInserted < 0 ||
                    CurrentStationIndexIgnoreInserted >= currentLeg.Stations.Count) return null;
                return currentLeg.Stations[CurrentStationIndexIgnoreInserted];
            }
        }

        private double CurrentLocation => _vehicleStateProvider.Location;
        public long Version { get; protected set; }

        /// <summary>
        ///     正常到站的时间戳
        /// </summary>
        public TimeSpan? CurrentStationSwitchTime { get; private set; }

        /// <summary>
        ///     获取下一个即将停靠的物理车站信息（自动跳过期间所有的通过站，若无后续停靠站则为 null）
        /// </summary>
        public TIMSStation<TSignal> NextStopStation { get; private set; }

        /// <summary>
        ///     获取进站提示被触发时的时间戳（如果尚未进入下一站提示区间则为 null）
        /// </summary>
        public TimeSpan? ArrivalHintTriggeredTime { get; private set; }

        /// <summary>
        ///     IC卡是否插入
        /// </summary>
        public abstract bool Inserted { get; protected set; }

        /// <summary>
        ///     获取运营区所
        /// </summary>
        public string Depot { get; protected set; }

        /// <summary>
        ///     获取行路番号，未插入 IC 卡时为空
        /// </summary>
        public string DutyNumber => Inserted ? _dutyNumber : string.Empty;

        /// <summary>
        ///     获取所有运用段列表
        /// </summary>
        public IReadOnlyList<TIMSLeg<TSignal>> Legs { get; protected set; }

        /// <summary>
        ///     获取当前的里程位置
        /// </summary>
        public double CurrentMileage { get; private set; }

        /// <summary>
        ///     当前缓行区间的里程范围（最多两个：正在通过的区间与紧随其后的区间）。
        ///     未插入 IC 卡或未处于任何缓行区间时两个均为 null
        /// </summary>
        public (TIMSSlowSection?, TIMSSlowSection?) CurrentSlowSections =>
            Inserted ? _currentSlowSections : (null, null);

        /// <summary>
        ///     获取当前运用段
        /// </summary>
        public TIMSLeg<TSignal> CurrentLeg
        {
            get
            {
                if (!Inserted || CurrentLegIndexIgnoreInserted < 0 || CurrentLegIndexIgnoreInserted >= Legs.Count)
                    return null;
                return Legs[CurrentLegIndexIgnoreInserted];
            }
        }

        /// <summary>
        ///     获取当前的车站定位信息
        /// </summary>
        public TIMSStation<TSignal> CurrentStation
        {
            get
            {
                var currentLeg = CurrentLeg;
                if (currentLeg == null || CurrentStationIndexIgnoreInserted < 0 ||
                    currentLeg.Stations.Count <= CurrentStationIndexIgnoreInserted) return null;
                return currentLeg.Stations[CurrentStationIndexIgnoreInserted];
            }
        }

        /// <summary>
        ///     获取当前运用段的降车站
        /// </summary>
        public TIMSStation<TSignal> CurrentAlightingStation => CurrentLeg?.Stations.LastOrDefault();

        /// <summary>
        ///     获取上一个已经通过的采时站信息（不包含当前车站，支持向前跨运用段检索，若无则为 null）
        /// </summary>
        public TIMSStation<TSignal> PrevTimingStation
        {
            get
            {
                if (!Inserted || CurrentLegIndexIgnoreInserted < 0 || CurrentLegIndexIgnoreInserted >= Legs.Count ||
                    CurrentStationIndexIgnoreInserted < 0)
                    return null;

                var currentLeg = CurrentLeg;
                if (currentLeg == null || CurrentStationIndexIgnoreInserted >= currentLeg.Stations.Count)
                    return null;

                var currentStation = currentLeg.Stations[CurrentStationIndexIgnoreInserted];

                if (currentStation.IsTimingStation &&
                    !IsStartingStation(CurrentLegIndexIgnoreInserted, CurrentStationIndexIgnoreInserted)) return null;

                var startIndex = Math.Min(Math.Max(CurrentStationIndexIgnoreInserted - 1, 0),
                    currentLeg.Stations.Count - 1);
                for (var i = startIndex; i >= 0; i--)
                {
                    var station = currentLeg.Stations[i];
                    if (IsStartingStation(CurrentLegIndexIgnoreInserted, i) ||
                        (station.StopType == TIMSStopType.Stop && station.IsTimingStation))
                        return station;
                }

                if (!IsStartingStation(CurrentLegIndexIgnoreInserted, 0))
                    for (var l = CurrentLegIndexIgnoreInserted - 1; l >= 0; l--)
                    {
                        var prevLeg = Legs[l];
                        if (prevLeg?.Stations == null) continue;

                        for (var i = prevLeg.Stations.Count - 1; i >= 0; i--)
                        {
                            var station = prevLeg.Stations[i];
                            if (IsStartingStation(l, i) ||
                                (station.StopType == TIMSStopType.Stop && station.IsTimingStation))
                                return station;
                        }
                    }

                return null;
            }
        }

        /// <summary>
        ///     获取当前运用段的列车番号
        /// </summary>
        public string CurrentTrainNumber =>
            Inserted ? CurrentLeg?.TrainNumber ?? Legs.FirstOrDefault()?.TrainNumber ?? string.Empty : string.Empty;

        /// <summary>
        ///     获取当前运用段的列车编成标识
        /// </summary>
        public string CurrentFormation =>
            CurrentLeg?.Formation ?? Legs.FirstOrDefault()?.Formation ?? DefaultFormation;

        public string CurrentRadioChannel => CurrentStationIgnoreInserted?.RadioChannel ??
                                             Legs.FirstOrDefault()?.Stations.FirstOrDefault()?.RadioChannel ??
                                             string.Empty;

        /// <summary>
        ///     获取当前运用段的屏幕显示类型
        /// </summary>
        public TIMSDisplayMode CurrentDisplayMode =>
            CurrentLeg?.DisplayMode ?? Legs.FirstOrDefault()?.DisplayMode ?? DefaultDisplayMode;

        /// <summary>
        ///     获取当前车站的运转速度
        /// </summary>
        public string CurrentStandardOperatingSpeed => CurrentStation?.StandardOperatingSpeed;

        /// <summary>
        ///     获取当前运用段的列车行先覆写设定
        /// </summary>
        public TIMSDestination CurrentOverrideDestination => CurrentLeg?.OverrideDestination;

        /// <summary>
        ///     获取当前运用段的下一行路任务，未插入 IC 卡时为 null
        /// </summary>
        public TIMSNextDuty NextDuty => Inserted ? CurrentLeg?.NextDuty ?? Legs.FirstOrDefault()?.NextDuty : null;

        private TSignal? ActiveSignalSystem
        {
            get
            {
                if (CurrentLegIndexIgnoreInserted < 0 || CurrentStationIndexIgnoreInserted < 0)
                    return DefaultSignalSystem;

                TIMSStation<TSignal> effectiveStation;
                double stationLoc = -1;
                if (_signalSystemSwitchedForCurrentStation)
                {
                    effectiveStation = CurrentStationIgnoreInserted;
                    if (effectiveStation != null) stationLoc = effectiveStation.MinLocation;
                }
                else
                {
                    effectiveStation = GetPreviousStation();
                    if (effectiveStation != null) stationLoc = effectiveStation.MinLocation;
                }

                double cpLoc = -1;
                var cp = GetMostRecentChangePoint(CurrentLocation);
                if (cp != null) cpLoc = cp.StartLocation;

                if (stationLoc >= 0 || cpLoc >= 0)
                {
                    if (cpLoc > stationLoc) return cp.SignalSystem;

                    if (effectiveStation != null) return effectiveStation.ActiveSignalSystem;
                }

                return DefaultSignalSystem;
            }
        }

        public TIMSTrainType? CurrentTrainType
        {
            get
            {
                if (CurrentLegIndex == 0 && CurrentStationIndex == 0) return Legs[0].Stations[0].TrainType;
                if (_trainTypeSwitchedForCurrentStation) return CurrentStation?.TrainType;
                var prevStation = GetPreviousStation();
                return prevStation?.TrainType;
            }
        }

        public virtual void Update(TimeSpan elapsed)
        {
            var prevStationIdx = CurrentStationIndexIgnoreInserted;
            UpdateDoorOpenHistory();
            FindRawCandidate(out var rawLeg, out var rawStn, false);
            if (rawLeg == -1)
            {
                SetIndices(-1, -1);
                UpdateCurrentMileage();
                UpdateCurrentSlowSections();
                SyncPassSetting();
                Sync(false);
                return;
            }

            var targetLeg = rawLeg;
            var targetStn = rawStn;

            if (rawLeg > 0 && rawStn == 0)
            {
                var prevLeg = Legs[rawLeg - 1];
                var prevLastStnIdx = prevLeg.Stations.Count - 1;
                var isOverlap = prevLeg.Stations[prevLastStnIdx].StationId == Legs[rawLeg].Stations[0].StationId;

                if (isOverlap)
                {
                    if (!_vehicleStateProvider.AreAllDoorClosed) _hasOpenedDoorAtCurrentStation = true;

                    if (_hasOpenedDoorAtCurrentStation && !_hasCompletedSwitchForCurrentStop)
                    {
                        _switchTimer += elapsed.TotalSeconds;
                        var requiredDelay = Legs[rawLeg].SwitchDuration;

                        if (_switchTimer >= requiredDelay)
                        {
                            targetLeg = rawLeg;
                            targetStn = 0;
                            _hasCompletedSwitchForCurrentStop = true;
                        }
                        else
                        {
                            targetLeg = rawLeg - 1;
                            targetStn = prevLastStnIdx;
                        }
                    }
                    else if (!_hasOpenedDoorAtCurrentStation)
                    {
                        if (CurrentLegIndexIgnoreInserted < rawLeg)
                        {
                            targetLeg = rawLeg - 1;
                            targetStn = prevLastStnIdx;
                        }
                        else
                        {
                            targetLeg = rawLeg;
                            targetStn = 0;
                        }
                    }
                }
            }

            if (targetLeg != CurrentLegIndexIgnoreInserted || targetStn != CurrentStationIndexIgnoreInserted)
            {
                SetIndices(targetLeg, targetStn);
                _switchTimer = 0;
                _signalSystemTimer = 0;
                _trainTypeTimer = 0;
                _signalSystemSwitchedForCurrentStation = false;
                _trainTypeSwitchedForCurrentStation = false;

                // 记录正常到站（即非ForceInstant切换）的时间戳
                CurrentStationSwitchTime = _timeProvider.CurrentTime;

                if (IsPhysicalStationChanged(targetLeg, targetStn))
                {
                    _hasOpenedDoorAtCurrentStation = false;
                    _hasCompletedSwitchForCurrentStop = false;
                }
            }

            if (CurrentLegIndexIgnoreInserted >= 0 && CurrentStationIndexIgnoreInserted >= 0)
            {
                var currentStation = CurrentStationIgnoreInserted;
                var isAlighting = CurrentLegIgnoreInserted != null &&
                                  CurrentStationIndexIgnoreInserted == CurrentLegIgnoreInserted.Stations.Count - 1;
                var conditionMet = _hasOpenedDoorAtCurrentStation || currentStation?.StopType == TIMSStopType.Pass;
                if (!isAlighting && conditionMet)
                {
                    if (!_signalSystemSwitchedForCurrentStation)
                    {
                        _signalSystemTimer += elapsed.TotalSeconds;
                        if (_signalSystemTimer >= currentStation.SignalSystemSwitchDuration)
                            _signalSystemSwitchedForCurrentStation = true;
                    }

                    if (!_trainTypeSwitchedForCurrentStation)
                    {
                        _trainTypeTimer += elapsed.TotalSeconds;
                        if (_trainTypeTimer >= currentStation.TrainTypeSwitchDuration)
                            _trainTypeSwitchedForCurrentStation = true;
                    }
                }
            }

            EvaluateArrivalHintTrigger();
            UpdateCurrentMileage();
            UpdateCurrentSlowSections();
            SyncPassSetting();
            Sync(false);
            return;

            void SyncPassSetting()
            {
                var currentStnIdx = CurrentStationIndexIgnoreInserted;
                if ((!Inserted || currentStnIdx < 0) && !_timsService.HasPassSetting)
                {
                    _timsService.ResetPassSetting();
                    _prevInserted = Inserted;
                }
                else if (Inserted != _prevInserted ||
                         (currentStnIdx >= 0 && prevStationIdx < 0))
                {
                    _prevInserted = Inserted;
                    _timsService.CompleteSetting();
                }
            }
        }

        protected void ApplyCard(string depot, string dutyNumber,
            IReadOnlyList<TIMSLeg<TSignal>> legs)
        {
            Depot = depot;
            _dutyNumber = dutyNumber;
            Legs = legs ?? Array.Empty<TIMSLeg<TSignal>>();
            Inserted = legs != null;
            ResetRuntimeState();
            Version++;
        }

        private void ResetRuntimeState()
        {
            _hasCompletedSwitchForCurrentStop = false;
            _hasOpenedDoorAtCurrentStation = false;
            _prevDoorLeg = -1;
            _prevDoorStation = -1;
            _prevFormation = null;
            _prevInserted = false;
            _signalSystemSwitchedForCurrentStation = false;
            _signalSystemTimer = 0;
            _switchTimer = 0;
            _trainTypeSwitchedForCurrentStation = false;
            _trainTypeTimer = 0;
            CurrentLegIndexIgnoreInserted = -1;
            CurrentStationIndexIgnoreInserted = -1;
            CurrentStationSwitchTime = null;
            ArrivalHintTriggeredTime = null;
            NextStopStation = null;
            CurrentMileage = CurrentLocation;
            _currentSlowSections = (null, null);
        }

        public bool IsStartingStation(int legIdx, int stationIdx)
        {
            if (!Inserted || legIdx < 0 || legIdx >= Legs.Count) return false;
            if (stationIdx != 0) return false;
            if (legIdx == 0) return true;
            return Legs[legIdx - 1].OverrideDestination == null;
        }

        public TIMSStation<TSignal> GetNextTimingStationAfter(int stationIndex)
        {
            if (!Inserted || CurrentLegIndexIgnoreInserted < 0 || CurrentLegIndexIgnoreInserted >= Legs.Count ||
                stationIndex < 0)
                return null;

            var currentLeg = CurrentLeg;
            if (currentLeg == null || stationIndex >= currentLeg.Stations.Count)
                return null;

            for (var i = stationIndex; i < currentLeg.Stations.Count; i++)
            {
                var station = currentLeg.Stations[i];
                if (station.StopType == TIMSStopType.Stop && station.IsTimingStation) return station;
            }

            return null;
        }

        private void SetIndices(int legIndex, int stationIndex)
        {
            if (CurrentLegIndexIgnoreInserted != legIndex || CurrentStationIndexIgnoreInserted != stationIndex)
            {
                CurrentLegIndexIgnoreInserted = legIndex;
                CurrentStationIndexIgnoreInserted = stationIndex;
                ArrivalHintTriggeredTime = null;
                NextStopStation = GetNextStopStation();
            }
        }

        private TIMSStation<TSignal> GetNextStopStation()
        {
            if (CurrentLegIndexIgnoreInserted < 0 || CurrentStationIndexIgnoreInserted < 0) return null;
            var currentLeg = CurrentLegIgnoreInserted;
            if (currentLeg == null) return null;
            for (var i = CurrentStationIndexIgnoreInserted + 1; i < currentLeg.Stations.Count; i++)
            {
                var station = currentLeg.Stations[i];
                if (station.StopType == TIMSStopType.Stop) return station;
            }

            return null;
        }

        private TIMSStation<TSignal> GetPreviousStation()
        {
            if (CurrentLegIndexIgnoreInserted < 0 || CurrentStationIndexIgnoreInserted < 0) return null;
            if (CurrentStationIndexIgnoreInserted > 0)
                return Legs[CurrentLegIndexIgnoreInserted].Stations[CurrentStationIndexIgnoreInserted - 1];

            if (CurrentLegIndexIgnoreInserted > 0)
            {
                var prevLeg = Legs[CurrentLegIndexIgnoreInserted - 1];
                return prevLeg.Stations[prevLeg.Stations.Count - 1];
            }

            return null;
        }

        private TIMSSignalSystemChangePoint<TSignal> GetMostRecentChangePoint(double currentLocation)
        {
            if (CurrentLegIndexIgnoreInserted < 0) return null;
            for (var l = CurrentLegIndexIgnoreInserted; l >= 0; l--)
            {
                var changePoints = Legs[l].SignalSystemChangePoints;
                if (changePoints == null || changePoints.Count == 0) continue;

                TIMSSignalSystemChangePoint<TSignal> bestCp = null;
                double maxLoc = -1;
                foreach (var cp in changePoints)
                    if (cp.StartLocation <= currentLocation && cp.StartLocation > maxLoc)
                    {
                        maxLoc = cp.StartLocation;
                        bestCp = cp;
                    }

                if (bestCp != null) return bestCp;
            }

            return null;
        }

        protected void ForceInstant(bool isHotReload = false)
        {
            UpdateDoorOpenHistory();

            int rawLeg;
            int rawStn;

            if (isHotReload)
                FindRawCandidateForReload(out rawLeg, out rawStn);
            else
                FindRawCandidate(out rawLeg, out rawStn, true);

            if (rawLeg == -1)
            {
                SetIndices(-1, -1);
                Clear();
                return;
            }

            var targetLeg = rawLeg;
            var targetStn = rawStn;

            if (rawLeg > 0 && rawStn == 0)
            {
                var prevLeg = Legs[rawLeg - 1];
                var prevLastStnIdx = prevLeg.Stations.Count - 1;
                var isOverlap = prevLeg.Stations[prevLastStnIdx].StationId == Legs[rawLeg].Stations[0].StationId;

                if (isOverlap && !_vehicleStateProvider.AreAllDoorClosed)
                {
                    targetLeg = rawLeg - 1;
                    targetStn = prevLastStnIdx;
                    _hasOpenedDoorAtCurrentStation = true;
                    _hasCompletedSwitchForCurrentStop = false;
                }
                else
                {
                    _hasOpenedDoorAtCurrentStation = false;
                    _hasCompletedSwitchForCurrentStop = false;
                }
            }
            else
            {
                _hasOpenedDoorAtCurrentStation = false;
                _hasCompletedSwitchForCurrentStop = false;
            }

            SetIndices(targetLeg, targetStn);
            if (isHotReload && targetLeg >= 0 && targetStn >= 0)
            {
                _prevDoorLeg = targetLeg;
                _prevDoorStation = targetStn;
            }

            Clear();
            return;

            void Clear()
            {
                _switchTimer = 0;
                _signalSystemTimer = 0;
                _trainTypeTimer = 0;

                CurrentStationSwitchTime = null;

                var hasCurrentStation = Inserted && CurrentStationIndexIgnoreInserted >= 0;
                if (hasCurrentStation)
                {
                    if (!_timsService.HasPassSetting) _timsService.CompleteSetting();
                }
                else
                {
                    _timsService.ResetPassSetting();
                }

                UpdateCurrentMileage();
                UpdateCurrentSlowSections();
                Sync(isHotReload);
            }
        }

        private void UpdateDoorOpenHistory()
        {
            if (_vehicleStateProvider.AreAllDoorClosed) return;
            for (var l = Legs.Count - 1; l >= 0; l--)
            for (var s = Legs[l].Stations.Count - 1; s >= 0; s--)
            {
                var station = Legs[l].Stations[s];
                if (CurrentLocation >= station.MinLocation && CurrentLocation <= station.MaxLocation)
                {
                    _prevDoorLeg = l;
                    _prevDoorStation = s;
                    _hasOpenedDoorAtCurrentStation = true;
                    return;
                }
            }
        }

        private void FindRawCandidateForReload(out int bestLeg, out int bestStn)
        {
            bestLeg = -1;
            bestStn = -1;

            if (Legs == null || Legs.Count == 0) return;
            for (var l = Legs.Count - 1; l >= 0; l--)
            for (var s = Legs[l].Stations.Count - 1; s >= 0; s--)
            {
                var station = Legs[l].Stations[s];
                if (station.MinLocation <= CurrentLocation)
                {
                    var isOverlap = IsOverlapStation(l, s);
                    if (isOverlap && !_hasOpenedDoorAtCurrentStation && CurrentLocation >= station.MinLocation &&
                        CurrentLocation <= station.MaxLocation && CurrentLegIndexIgnoreInserted < l)
                        continue;

                    bestLeg = l;
                    bestStn = s;
                    return;
                }
            }
        }

        private void FindRawCandidate(out int bestLeg, out int bestStn, bool isForceSet)
        {
            bestLeg = -1;
            bestStn = -1;

            var msLeg = -1;
            var msStn = -1;

            for (var l = Legs.Count - 1; l >= 0; l--)
            for (var s = Legs[l].Stations.Count - 1; s >= 0; s--)
            {
                var station = Legs[l].Stations[s];
                if (station.SwitchMode == TIMSStationSwitchMode.MinStopPosition &&
                    station.MinLocation <= CurrentLocation)
                {
                    var isOverlap = IsOverlapStation(l, s);
                    if (isOverlap && !_hasOpenedDoorAtCurrentStation && CurrentLocation >= station.MinLocation &&
                        CurrentLocation <= station.MaxLocation && (isForceSet || CurrentLegIndexIgnoreInserted < l))
                        continue;

                    msLeg = l;
                    msStn = s;
                    goto MinStopSearchEnd;
                }
            }

            MinStopSearchEnd:

            var hasMs = msLeg != -1;
            var hasDoor = _prevDoorLeg != -1 &&
                          Legs[_prevDoorLeg].Stations[_prevDoorStation].MinLocation <= CurrentLocation;

            if (hasMs && hasDoor)
            {
                var msLoc = Legs[msLeg].Stations[msStn].MinLocation;
                var doorLoc = Legs[_prevDoorLeg].Stations[_prevDoorStation].MinLocation;

                if (msLoc > doorLoc)
                {
                    bestLeg = msLeg;
                    bestStn = msStn;
                }
                else if (doorLoc > msLoc)
                {
                    bestLeg = _prevDoorLeg;
                    bestStn = _prevDoorStation;
                }
                else
                {
                    if (_prevDoorLeg > msLeg || (_prevDoorLeg == msLeg && _prevDoorStation > msStn))
                    {
                        bestLeg = _prevDoorLeg;
                        bestStn = _prevDoorStation;
                    }
                    else
                    {
                        bestLeg = msLeg;
                        bestStn = msStn;
                    }
                }
            }
            else if (hasMs)
            {
                bestLeg = msLeg;
                bestStn = msStn;
            }
            else if (hasDoor)
            {
                bestLeg = _prevDoorLeg;
                bestStn = _prevDoorStation;
            }

            if (bestLeg == -1)
                if (CurrentLegIndexIgnoreInserted >= 0 && CurrentLegIndexIgnoreInserted < Legs.Count)
                {
                    var stations = Legs[CurrentLegIndexIgnoreInserted].Stations;
                    var closestStn = -1;
                    var maxLocBeforeCurrent = double.MinValue;

                    for (var s = 0; s < stations.Count; s++)
                    {
                        var loc = stations[s].MinLocation;
                        if (loc <= CurrentLocation && loc > maxLocBeforeCurrent)
                        {
                            maxLocBeforeCurrent = loc;
                            closestStn = s;
                        }
                    }

                    if (closestStn != -1)
                    {
                        var isOverlapStation = IsOverlapStation(CurrentLegIndexIgnoreInserted, closestStn);

                        if (isOverlapStation)
                        {
                            if (isForceSet)
                            {
                                var prevDoorIdx =
                                    FindPreviousDoorOpenStationIndex(CurrentLegIndexIgnoreInserted, closestStn);
                                if (prevDoorIdx != -1)
                                {
                                    bestLeg = CurrentLegIndexIgnoreInserted;
                                    bestStn = prevDoorIdx;
                                    _prevDoorLeg = CurrentLegIndexIgnoreInserted;
                                    _prevDoorStation = prevDoorIdx;
                                }
                            }
                            else
                            {
                                bestLeg = CurrentLegIndexIgnoreInserted;
                                bestStn = closestStn;
                            }
                        }
                    }
                }
        }

        public bool IsOverlapStation(int legIdx, int stationIdx)
        {
            if (stationIdx == Legs[legIdx].Stations.Count - 1 && legIdx < Legs.Count - 1)
                return Legs[legIdx].Stations[stationIdx].StationId == Legs[legIdx + 1].Stations[0].StationId;

            if (stationIdx == 0 && legIdx > 0)
            {
                var prevLegLastIdx = Legs[legIdx - 1].Stations.Count - 1;
                return Legs[legIdx].Stations[0].StationId == Legs[legIdx - 1].Stations[prevLegLastIdx].StationId;
            }

            return false;
        }

        private int FindPreviousDoorOpenStationIndex(int legIdx, int fromStationIdx)
        {
            var stations = Legs[legIdx].Stations;
            for (var s = fromStationIdx - 1; s >= 0; s--)
                if (stations[s].SwitchMode == TIMSStationSwitchMode.DoorOpen)
                    return s;

            return -1;
        }

        private bool IsPhysicalStationChanged(int targetLeg, int targetStn)
        {
            if (CurrentLegIndexIgnoreInserted < 0 || CurrentLegIndexIgnoreInserted >= Legs.Count || targetLeg < 0 ||
                targetLeg >= Legs.Count)
                return true;

            return Legs[CurrentLegIndexIgnoreInserted].Stations[CurrentStationIndexIgnoreInserted].StationId !=
                   Legs[targetLeg].Stations[targetStn].StationId;
        }

        private void Sync(bool isHotReload)
        {
            var formation = CurrentFormation;
            if (formation != _prevFormation)
                if (FormationSpecs.TryGetValue(formation, out var formationSpec))
                {
                    _carStateService.Initialize(formationSpec.CarCount);
                    _doorStateService.Initialize(formationSpec.CarCount, formationSpec.DoorCountPerCar);
                    _passengerStateService.Initialize(formationSpec.UnitCarCounts);
                    _timsService.Initialize(formationSpec);
                    OnFormationChanged(formation, formationSpec, isHotReload);
                }

            _signalController.SetActiveSignalSystem(ActiveSignalSystem);
            _prevFormation = formation;
        }

        protected virtual void OnFormationChanged(string formation, TIMSFormationSpec formationSpec, bool isHotReload)
        {
        }

        private void EvaluateArrivalHintTrigger()
        {
            if (!Inserted) return;

            var nextStopStation = NextStopStation;

            if (nextStopStation != null && nextStopStation.ArrivalHintOffset > 0 && ArrivalHintTriggeredTime == null)
            {
                var distance = nextStopStation.MinLocation - CurrentLocation;
                if (distance > 0 && distance <= nextStopStation.ArrivalHintOffset)
                    ArrivalHintTriggeredTime = _timeProvider.CurrentTime;
            }
        }

        private TIMSMileageCorrectionPoint GetMostRecentCorrectionPoint(double currentLocation)
        {
            if (CurrentLegIndexIgnoreInserted < 0) return null;
            for (var l = CurrentLegIndexIgnoreInserted; l >= 0; l--)
            {
                var correctionPoints = Legs[l].MileageCorrectionPoints;
                if (correctionPoints == null || correctionPoints.Count == 0) continue;

                TIMSMileageCorrectionPoint bestCp = null;
                double maxLoc = -1;
                foreach (var cp in correctionPoints)
                    if (cp.Location <= currentLocation && cp.Location > maxLoc)
                    {
                        maxLoc = cp.Location;
                        bestCp = cp;
                    }

                if (bestCp != null) return bestCp;
            }

            return null;
        }

        private TIMSStation<TSignal> GetMostRecentStationWithMileage(double currentLocation)
        {
            if (CurrentLegIndexIgnoreInserted < 0) return null;
            for (var l = CurrentLegIndexIgnoreInserted; l >= 0; l--)
            {
                var stations = Legs[l].Stations;
                if (stations == null || stations.Count == 0) continue;

                TIMSStation<TSignal> bestStation = null;
                double maxLoc = -1;

                var maxIndex = l == CurrentLegIndexIgnoreInserted
                    ? CurrentStationIndexIgnoreInserted
                    : stations.Count - 1;
                for (var s = maxIndex; s >= 0; s--)
                {
                    var station = stations[s];
                    if (station.Mileage.HasValue && station.MinLocation <= currentLocation &&
                        station.MinLocation > maxLoc)
                    {
                        maxLoc = station.MinLocation;
                        bestStation = station;
                    }
                }

                if (bestStation != null) return bestStation;
            }

            return null;
        }

        private void UpdateCurrentMileage()
        {
            if (CurrentLegIndexIgnoreInserted < 0 || CurrentStationIndexIgnoreInserted < 0)
            {
                CurrentMileage = CurrentLocation;
                return;
            }

            var cp = GetMostRecentCorrectionPoint(CurrentLocation);
            var st = GetMostRecentStationWithMileage(CurrentLocation);
            var cpLoc = cp?.Location ?? -1;
            var stLoc = st?.MinLocation ?? -1;
            if (cpLoc >= 0 || stLoc >= 0)
            {
                if (cpLoc >= stLoc)
                {
                    var sign = cp.MileageDirection == TIMSMileageDirection.Increment ? 1 : -1;
                    CurrentMileage = cp.RemappedMileage + sign * (CurrentLocation - cp.Location);
                }
                else
                {
                    var sign = st.MileageDirection == TIMSMileageDirection.Increment ? 1 : -1;
                    CurrentMileage = st.Mileage.Value + sign * (CurrentLocation - st.MinLocation);
                }
            }
            else
            {
                CurrentMileage = CurrentLocation;
            }
        }

        private void UpdateCurrentSlowSections()
        {
            _currentSlowSections = (null, null);
            if (CurrentLegIndexIgnoreInserted < 0) return;

            var currentLeg = Legs[CurrentLegIndexIgnoreInserted];
            if (currentLeg.SlowSectionLocations == null || currentLeg.SlowSectionLocations.Count == 0) return;
            double trainLength = 0;
            if (FormationSpecs.TryGetValue(CurrentFormation, out var formationSpec))
                trainLength = formationSpec.CarCount * SlowSectionCarLengthMeters;
            var head = CurrentLocation;
            var tail = head - trainLength;
            // 前后顺序只按 location 判定：填充序号后原地排序
            var count = currentLeg.SlowSectionLocations.Count;
            _slowSectionOrdered.Clear();
            for (var i = 0; i < count; i++) _slowSectionOrdered.Add(i);
            for (var i = 1; i < count; i++)
            {
                var key = _slowSectionOrdered[i];
                var j = i - 1;
                while (j >= 0)
                {
                    var sa = currentLeg.SlowSectionLocations[_slowSectionOrdered[j]];
                    var sb = currentLeg.SlowSectionLocations[key];
                    var c = sa.startLocation.CompareTo(sb.startLocation);
                    if (c < 0 || (c == 0 && sa.endLocation <= sb.endLocation)) break;
                    _slowSectionOrdered[j + 1] = _slowSectionOrdered[j];
                    j--;
                }

                _slowSectionOrdered[j + 1] = key;
            }

            // 沿轨道 0..当前leg 枚举参考点（校正点 + 带里程的站），按 Location 升序。
            _slowSectionReferences.Clear();
            for (var l = 0; l <= CurrentLegIndexIgnoreInserted; l++)
            {
                foreach (var cp in Legs[l].MileageCorrectionPoints)
                    _slowSectionReferences.Add((cp.Location, cp.RemappedMileage,
                        cp.MileageDirection == TIMSMileageDirection.Increment ? 1.0 : -1.0, true));
                foreach (var station in Legs[l].Stations)
                    if (station.Mileage.HasValue)
                        _slowSectionReferences.Add((station.MinLocation, station.Mileage.Value,
                            station.MileageDirection == TIMSMileageDirection.Increment ? 1.0 : -1.0, false));
            }

            for (var i = 1; i < _slowSectionReferences.Count; i++)
            {
                var key = _slowSectionReferences[i];
                var j = i - 1;
                while (j >= 0 && _slowSectionReferences[j].Location > key.Location)
                {
                    _slowSectionReferences[j + 1] = _slowSectionReferences[j];
                    j--;
                }

                _slowSectionReferences[j + 1] = key;
            }

            // 把各徐行区间在断里程参考点处切成子区间，展平为按 location 升序的子区间序列
            _slowSectionSub.Clear();
            foreach (var si in _slowSectionOrdered)
            {
                var sec = currentLeg.SlowSectionLocations[si];
                _slowSectionSplits.Clear();
                _slowSectionSplits.Add(sec.startLocation);
                for (var i = 0; i < _slowSectionReferences.Count; i++)
                {
                    var r = _slowSectionReferences[i];
                    if (r.Location > sec.startLocation && r.Location < sec.endLocation)
                        _slowSectionSplits.Add(r.Location);
                }

                _slowSectionSplits.Add(sec.endLocation);
                // 已升序：start < 各断点 < end
                for (var k = 0; k < _slowSectionSplits.Count - 1; k++)
                    _slowSectionSub.Add((_slowSectionSplits[k], _slowSectionSplits[k + 1], sec.speedLimit));
            }

            // 当前子区间 = 位置最靠前且正在被通过（zhead>=start 且 tail<=end）者；下一子区间 = 其后未被车尾通过者
            int? currentIdx = null;
            for (var i = 0; i < _slowSectionSub.Count; i++)
            {
                var (sl, el, _) = _slowSectionSub[i];
                if (head >= sl && tail <= el)
                {
                    currentIdx = i;
                    break;
                }
            }

            if (currentIdx == null) return;

            TIMSSlowSection? slot2 = null;
            for (var k = currentIdx.Value + 1; k < _slowSectionSub.Count; k++)
            {
                var (_, el, _) = _slowSectionSub[k];
                if (tail > el) continue;
                slot2 = BuildSubInterval(_slowSectionSub[k], trainLength);
                break;
            }

            _currentSlowSections = (BuildSubInterval(_slowSectionSub[currentIdx.Value], trainLength), slot2);
        }

        // 单个子区间换算成里程：子区间自身落在单一参考系内；运行前方额外加上车长
        private TIMSSlowSection BuildSubInterval((double start, double end, int speedLimit) sub, double trainLength)
        {
            var anchor = GetReferenceAt(sub.start, _slowSectionReferences);

            var mLo = anchor.HasValue
                ? anchor.Value.MileageBase + anchor.Value.Sign * (sub.start - anchor.Value.Location)
                : sub.start;
            var mFar = anchor.HasValue
                ? anchor.Value.MileageBase + anchor.Value.Sign * (sub.end - anchor.Value.Location)
                : sub.end;
            var farSign = anchor?.Sign ?? 1.0;
            mFar += farSign * trainLength;

            return new TIMSSlowSection((int)Math.Round(Math.Min(mLo, mFar)),
                (int)Math.Round(Math.Max(mLo, mFar)), sub.speedLimit);
        }

        // 指定位置所在参考系：最近的（Location <= 位置）带里程站/校正点；同位置时校正点优先
        private static (double Location, double MileageBase, double Sign)? GetReferenceAt(double location,
            List<(double Location, double MileageBase, double Sign, bool IsCorrection)> references)
        {
            (double Location, double MileageBase, double Sign, bool IsCorrection)? best = null;
            foreach (var r in references)
            {
                if (r.Location > location) break;
                if (!best.HasValue)
                {
                    best = r;
                    continue;
                }

                if (r.Location > best.Value.Location)
                    best = r;
                else if (Math.Abs(r.Location - best.Value.Location) < Epsilons.DoubleEpsilon
                         && r.IsCorrection && !best.Value.IsCorrection)
                    best = r;
            }

            if (!best.HasValue) return null;
            return (best.Value.Location, best.Value.MileageBase, best.Value.Sign);
        }
    }
}