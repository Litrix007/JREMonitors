using System;
using System.Linq;
using JREMonitors.E233.TIMS.ICCard;

namespace JREMonitors.BveEx.Configs.Vehicle.TIMS
{
    public class TIMSICCardConfig
    {
        private string _depot = "";
        private string _dutyNumber = "";
        private TIMSLegConfig[] _legs = Array.Empty<TIMSLegConfig>();

        /// <summary>
        ///     运营区所。
        /// </summary>
        public string Depot
        {
            get => _depot;
            set => _depot = value ?? "";
        }

        /// <summary>
        ///     行路番号。
        /// </summary>
        public string DutyNumber
        {
            get => _dutyNumber;
            set => _dutyNumber = value ?? "";
        }

        /// <summary>
        ///     运用列表。
        /// </summary>
        public TIMSLegConfig[] Legs
        {
            get => _legs;
            set => _legs = value ?? Array.Empty<TIMSLegConfig>();
        }
    }

    public class TIMSLegConfig
    {
        private TIMSRouteNodeConfig[] _routeNodes = Array.Empty<TIMSRouteNodeConfig>();

        /// <summary>
        ///     编组名称。支持的值随车型变化。
        /// </summary>
        public string Formation { get; set; }

        /// <summary>
        ///     列车番号。
        /// </summary>
        public string TrainNumber { get; set; }

        /// <summary>
        ///     对应"?列番"。
        /// </summary>
        public string TrainNumberChar { get; set; }

        /// <summary>
        ///     画面类型。
        /// </summary>
        public TIMSDisplayMode DisplayMode { get; set; }

        /// <summary>
        ///     列车行先（在降车站和目的地不同时使用）。
        /// </summary>
        public TIMSDestinationConfig OverrideDestination { get; set; }

        /// <summary>
        ///     次行路。
        /// </summary>
        public TIMSNextDutyConfig NextDuty { get; set; }

        /// <summary>
        ///     切换等待时间（仅在非首个运用、且当前首站与上运用降车站重叠时生效）。
        /// </summary>
        public int SwitchDuration { get; set; }

        /// <summary>
        ///     节点列表。
        /// </summary>
        public TIMSRouteNodeConfig[] RouteNodes
        {
            get => _routeNodes;
            set => _routeNodes = value?.Where(e => e != null).ToArray() ?? Array.Empty<TIMSRouteNodeConfig>();
        }
    }

    public class TIMSDestinationConfig
    {
        private string _name = "";

        /// <summary>
        ///     终点站名称。
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? "";
        }
    }

    public class TIMSNextDutyConfig
    {
        /// <summary>
        ///     列车番号。
        /// </summary>
        public string TrainNumber { get; set; }

        /// <summary>
        ///     到着时刻。
        /// </summary>
        public TimeSpan? ArrivalTime { get; set; }

        /// <summary>
        ///     发车时刻。
        /// </summary>
        public TimeSpan? DepartureTime { get; set; }
    }
}