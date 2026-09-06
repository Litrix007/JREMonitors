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

        /// <summary>
        ///     TIMS 18号点阵字体名，未设置、不存在、缺字形会回退到<c>MS Gothic</c>。
        /// </summary>
        /// <remarks>
        ///     <para>此为监视器构建期定型配置，更改此属性将重建整个监视器系统。</para>
        /// </remarks>
        [JsonPropertyName("timsFont18Family")]
        public string TIMSFont18Family { get; set; }

        /// <summary>
        ///     M电/E电的秒数文字的Y轴偏移量，默认为<c>1</c>（适配<c>MS Gothic</c>），自定义字体对位不齐时调整。
        /// </summary>
        /// <remarks>
        ///     <para>此为监视器构建期定型配置，更改此属性将重建整个监视器系统。</para>
        /// </remarks>
        public int? TimeTableSecondsOffsetY { get; set; }
    }
}