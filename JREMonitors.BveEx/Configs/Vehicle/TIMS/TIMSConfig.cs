using System.Text.Json.Serialization;
using JREMonitors.E233.TIMS;

namespace JREMonitors.BveEx.Configs.Vehicle.TIMS
{
    public class TIMSConfig
    {
        /// <summary>
        ///     车辆行驶方向。
        /// </summary>
        public TIMSVehicleDirection VehicleDirection { get; set; } = TIMSVehicleDirection.Left;

        /// <summary>
        ///     室温基准值。
        /// </summary>
        public float? BaseInteriorTemperature { get; set; }

        /// <summary>
        ///     外温基准值。
        /// </summary>
        public float? ExternalTemperature { get; set; }

        /// <summary>
        ///     湿度基准值。
        /// </summary>
        public float? BaseHumidity { get; set; }

        /// <summary>
        ///     IC卡路径。
        /// </summary>
        /// <remarks>
        ///     启用热重载时，路径及对应内容变更时均可自动响应。
        /// </remarks>
        [JsonPropertyName("icCardPath")]
        public ConfigPath ICCardPath { get; set; }
    }
}