using System.Collections.Generic;
using JREMonitors.Core.Utils;

namespace JREMonitors.JRE.Constants
{
    public static class LogicalInputIds
    {
        /// <summary>
        ///     TASC切
        /// </summary>
        public static readonly string TascTurnOff = nameof(TascTurnOff).ToCamelCase();

        #region ATC

        /// <summary>
        ///     ATC速度制限
        /// </summary>
        public static readonly string AtcSpeedLimit = nameof(AtcSpeedLimit).ToCamelCase();

        /// <summary>
        ///     入換
        /// </summary>
        public static readonly string AtcShunt = nameof(AtcShunt).ToCamelCase();

        /// <summary>
        ///     絶対停止
        /// </summary>
        public static readonly string AtcAbsoluteStop = nameof(AtcAbsoluteStop).ToCamelCase();

        /// <summary>
        ///     パターン接近
        /// </summary>
        public static readonly string AtcPatternApproach = nameof(AtcPatternApproach).ToCamelCase();

        /// <summary>
        ///     インチング制御中
        /// </summary>
        public static readonly string InchingActivated = nameof(InchingActivated).ToCamelCase();

        /// <summary>
        ///     切
        /// </summary>
        public static readonly string AtcTurnOff = nameof(AtcTurnOff).ToCamelCase();

        /// <summary>
        ///     パターン低滅
        /// </summary>
        public static readonly string PatternCleared = nameof(PatternCleared).ToCamelCase();

        /// <summary>
        ///     非常運転
        /// </summary>
        public static readonly string EmergencyRun = nameof(EmergencyRun).ToCamelCase();

        /// <summary>
        ///     ATC常用
        /// </summary>
        public static readonly string AtcServiceBrake = nameof(AtcServiceBrake).ToCamelCase();

        /// <summary>
        ///     ATC非常
        /// </summary>
        public static readonly string AtcEmergencyBrake = nameof(AtcEmergencyBrake).ToCamelCase();

        /// <summary>
        ///     停通防止動作
        /// </summary>
        public static readonly string OverrunAction = nameof(OverrunAction).ToCamelCase();

        /// <summary>
        ///     ATC電源
        /// </summary>
        public static readonly string AtcPower = nameof(AtcPower).ToCamelCase();

        /// <summary>
        ///     ATC開放
        /// </summary>
        public static readonly string AtcCutout = nameof(AtcCutout).ToCamelCase();

        public static readonly IReadOnlyList<string> NormalAtcLampIds = new List<string>
        {
            AtcShunt, AtcAbsoluteStop, AtcPatternApproach, InchingActivated, AtcTurnOff, PatternCleared,
            EmergencyRun, AtcServiceBrake, AtcEmergencyBrake, OverrunAction, AtcPower, AtcCutout
        };

        #endregion
    }
}