using System;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.ICCard
{
    public class TIMSStation<TSignal> where TSignal : struct, Enum
    {
        public TIMSStation(
            string stationId,
            string name,
            double? mileage,
            TIMSMileageDirection mileageDirection,
            double minLocation,
            double maxLocation,
            TIMSStationSwitchMode switchMode,
            TSignal? activeSignalSystem
        )
        {
            StationId = stationId;
            Name = name;
            Mileage = mileage;
            MileageDirection = mileageDirection;
            MinLocation = minLocation;
            MaxLocation = maxLocation;
            SwitchMode = switchMode;
            ActiveSignalSystem = activeSignalSystem;
        }

        public string StationId { get; }
        public string Name { get; }
        public double? Mileage { get; }
        public TIMSMileageDirection MileageDirection { get; }
        public double MinLocation { get; }
        public double Location => (MaxLocation + MinLocation) / 2;
        public double MaxLocation { get; }

        /// <summary>
        ///     切至当前站的时机
        /// </summary>
        public TIMSStationSwitchMode SwitchMode { get; }

        public bool ShowStopText { get; set; }

        public TSignal? ActiveSignalSystem { get; }

        /// <summary>
        ///     番线
        /// </summary>
        public string TrackName { get; set; }

        public TIMSStopType StopType { get; set; }

        /// <summary>
        ///     显示的到着时刻
        /// </summary>
        public TimeSpan? ArrivalTime { get; set; }

        /// <summary>
        ///     显示的发车时刻
        /// </summary>
        public TimeSpan? DepartureTime { get; set; }

        public int StopDuration { get; set; }
        public Color3? Color { get; set; }
        public Color3? TrackColor { get; set; }

        /// <summary>
        ///     站名文字颜色。仅在E电生效。
        /// </summary>
        public Color3? EDenTextColor { get; set; }

        /// <summary>
        ///     当前站距下一站的位置指示线颜色，仅在E电生效
        /// </summary>
        public Color3? LineColor { get; set; }

        /// <summary>
        ///     当前站距下一站的位置指示线宽度，仅在E电生效
        /// </summary>
        public int LineStrokeWidth { get; set; }

        public string RadioChannel { get; set; }

        /// <summary>
        ///     运转速度，仅在E电生效
        /// </summary>
        public string StandardOperatingSpeed { get; set; }

        /// <summary>
        ///     进站提示距站点的偏移量
        /// </summary>
        public double ArrivalHintOffset { get; set; }

        public TIMSStationTask StationTask { get; set; }

        /// <summary>
        ///     进站限速，仅在M电生效
        /// </summary>
        public string SpeedLimitArrival { get; set; }

        /// <summary>
        ///     出站限速，仅在M电生效
        /// </summary>
        public string SpeedLimitDeparture { get; set; }

        /// <summary>
        ///     是否为采时站
        /// </summary>
        public bool IsTimingStation { get; set; }

        public TIMSTrainType? TrainType { get; set; }

        public int SignalSystemSwitchDuration { get; set; }

        public int TrainTypeSwitchDuration { get; set; }

        /// <summary>
        ///     站内闭塞起点距车站位置的偏移量
        /// </summary>
        public int StationBlockStartOffset { get; set; } = 100;

        /// <summary>
        ///     站内闭塞终点距车站位置的偏移量
        /// </summary>
        public int StationBlockEndOffset { get; set; }
    }
}