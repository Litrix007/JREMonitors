using System;
using System.Text.Json.Serialization;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using Vortice.Mathematics;

namespace JREMonitors.BveEx.Configs.Vehicle.TIMS
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TIMSMileageCorrectionPointConfig), "mileageCorrection")]
    [JsonDerivedType(typeof(TIMSSlowSectionConfig), "slowSection")]
    public abstract class TIMSRouteNodeConfig
    {
    }

    public class TIMSStationConfig<TSignal> : TIMSRouteNodeConfig where TSignal : struct, Enum
    {
        private TSignal? _activeSignalSystem;
        private int _lineStrokeWidth = 1;
        private string _radioChannel = "";
        private string _speedLimitArrival = "";
        private string _speedLimitDeparture = "";
        private string _standardOperatingSpeed = "";
        private int _stationBlockEndOffset;
        private int _stationBlockStartOffset;
        private string _trackName = "";
        private TIMSTrainType? _trainType;

        /// <summary>
        ///     车站ID，用于匹配游戏线路中的车站。
        /// </summary>
        public string StationId { get; set; }

        /// <summary>
        ///     站名显示文本，未配置时使用游戏线路中的车站名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     是否为采时站，未配置时根据是否配置了到着/发车时刻自动推断。
        /// </summary>
        public bool? IsTimingStation { get; set; }

        /// <summary>
        ///     覆盖到着时刻，未配置时使用游戏车站的到着时刻。
        /// </summary>
        public TimeSpan? OverrideArrivalTime { get; set; }

        /// <summary>
        ///     运转时分。
        /// </summary>
        public int? OverrideStopDuration { get; set; }

        /// <summary>
        ///     覆盖发车时刻，未配置时使用游戏车站的发车时刻；终点站恒不生效。
        /// </summary>
        public TimeSpan? OverrideDepartureTime { get; set; }

        /// <summary>
        ///     切至当前站的时机。
        /// </summary>
        public TIMSStationSwitchMode? SwitchMode { get; set; }

        /// <summary>
        ///     停站时是否显示“停”字样，仅在该站为停车站时生效。
        /// </summary>
        public bool ShowStopText { get; set; }

        /// <summary>
        ///     运转速度，仅在E电生效；未配置时沿用上一站的值。
        /// </summary>
        public string StandardOperatingSpeed
        {
            get => _standardOperatingSpeed;
            set
            {
                _standardOperatingSpeed = value ?? "";
                IsStandardOperatingSpeedExplicitlySet = true;
            }
        }

        public bool IsStandardOperatingSpeedExplicitlySet { get; private set; }

        /// <summary>
        ///     番线。
        /// </summary>
        public string TrackName
        {
            get => _trackName;
            set => _trackName = value ?? "";
        }

        /// <summary>
        ///     该站对应的重映射里程，作为徐行区间等里程换算的参照点。
        /// </summary>
        public double? RemappedMileage { get; set; } = null;

        /// <summary>
        ///     番线显示颜色。
        /// </summary>
        public Color3? TrackColor { get; set; }

        /// <summary>
        ///     站名颜色，在M电时为文字颜色，在E电时为背景颜色。
        /// </summary>
        public Color3? Color { get; set; }

        /// <summary>
        ///     站名文字颜色。仅在E电生效。
        /// </summary>
        public Color3? EDenTextColor { get; set; }

        /// <summary>
        ///     当前站距下一站的位置指示线颜色，仅在E电生效。
        /// </summary>
        public Color3? LineColor { get; set; }

        /// <summary>
        ///     当前站距下一站的位置指示线宽度，仅在E电生效。
        /// </summary>
        public int LineStrokeWidth
        {
            get => _lineStrokeWidth;
            set => _lineStrokeWidth = MathHelper.Clamp(value, 1, 3);
        }

        /// <summary>
        ///     进站提示距站点的偏移量。
        /// </summary>
        public double ArrivalHintOffset { get; set; }

        /// <summary>
        ///     无线电频道，未配置时沿用上一站的值。
        /// </summary>
        public string RadioChannel
        {
            get => _radioChannel;
            set
            {
                _radioChannel = value ?? "";
                IsRadioChannelExplicitlySet = true;
            }
        }

        public bool IsRadioChannelExplicitlySet { get; private set; }

        /// <summary>
        ///     进站限速，仅在M电生效。
        /// </summary>
        public string SpeedLimitArrival
        {
            get => _speedLimitArrival;
            set => _speedLimitArrival = value ?? "";
        }

        /// <summary>
        ///     出站限速，仅在M电生效。
        /// </summary>
        public string SpeedLimitDeparture
        {
            get => _speedLimitDeparture;
            set => _speedLimitDeparture = value ?? "";
        }

        /// <summary>
        ///     控制里程加算/减算，默认为加算。
        /// </summary>
        public TIMSMileageDirection MileageDirection { get; set; } = TIMSMileageDirection.Increment;

        /// <summary>
        ///     车站作业任务（待/整/分/併），默认不显示。
        /// </summary>
        public TIMSStationTask StationTask { get; set; } = TIMSStationTask.None;

        /// <summary>
        ///     停站类型（停车/通过），未配置时根据游戏车站自动推断。
        /// </summary>
        public TIMSStopType? StopType { get; set; } = null;

        /// <summary>
        ///     该站起生效的信号系统，仅在该属性被显式配置时生效，未配置时沿用默认信号系统。
        /// </summary>
        public TSignal? ActiveSignalSystem
        {
            get => _activeSignalSystem;
            set
            {
                _activeSignalSystem = value;
                IsActiveSignalSystemExplicitlySet = true;
            }
        }

        public bool IsActiveSignalSystemExplicitlySet { get; private set; }

        /// <summary>
        ///     该站起适用的列车种别，未配置时沿用上一站的值。
        /// </summary>
        public TIMSTrainType? TrainType
        {
            get => _trainType;
            set
            {
                _trainType = value;
                IsTrainTypeExplicitlySet = true;
            }
        }

        public bool IsTrainTypeExplicitlySet { get; private set; }

        /// <summary>
        ///     信号系统切换等待时间。
        /// </summary>
        public int SignalSystemSwitchDuration { get; set; }

        /// <summary>
        ///     列车种别切换等待时间。
        /// </summary>
        public int TrainTypeSwitchDuration { get; set; }

        /// <summary>
        ///     站内闭塞起点距车站位置的偏移量。
        /// </summary>
        public int StationBlockStartOffset
        {
            get => _stationBlockStartOffset;
            set => _stationBlockStartOffset = MathHelper.Max(value, 0);
        }

        /// <summary>
        ///     站内闭塞终点距车站位置的偏移量。
        /// </summary>
        public int StationBlockEndOffset
        {
            get => _stationBlockEndOffset;
            set => _stationBlockEndOffset = MathHelper.Max(value, 0);
        }
    }

    public class TIMSSlowSectionConfig : TIMSRouteNodeConfig
    {
        /// <summary>
        ///     徐行区间相对于游戏地图的起始位置。
        /// </summary>
        public int StartLocation { get; set; }

        /// <summary>
        ///     徐行区间相对于游戏地图的结束位置。
        /// </summary>
        /// <remarks>
        ///     <para>实际终止点会自动加上编组长度。</para>
        /// </remarks>
        public int EndLocation { get; set; }

        /// <summary>
        ///     徐行区间限速。
        /// </summary>
        public int SpeedLimit { get; set; }
    }

    public class TIMSMileageCorrectionPointConfig : TIMSRouteNodeConfig
    {
        /// <summary>
        ///     断里程矫正点相对于游戏地图的位置。
        /// </summary>
        public int Location { get; set; }

        /// <summary>
        ///     重映射里程。
        /// </summary>
        public int RemappedMileage { get; set; }

        /// <summary>
        ///     控制里程加算/减算。默认为加算。
        /// </summary>
        public TIMSMileageDirection MileageDirection { get; set; } = TIMSMileageDirection.Increment;
    }

    public class TIMSSignalSystemChangePointConfig<TSignal> : TIMSRouteNodeConfig where TSignal : struct, Enum
    {
        /// <summary>
        ///     切换点相对于游戏地图的位置。
        /// </summary>
        public int StartLocation { get; }

        /// <summary>
        ///     切换点后生效的信号系统。
        /// </summary>
        public TSignal SignalSystem { get; }
    }
}