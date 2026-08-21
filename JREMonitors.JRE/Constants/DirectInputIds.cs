using System.Collections.Generic;
using JREMonitors.Core.Utils;

namespace JREMonitors.JRE.Constants
{
    public static class DirectInputIds
    {
        public static readonly string DeviceVoltage = nameof(DeviceVoltage).ToCamelCase();

        /// <summary>
        ///     P電源
        /// </summary>
        public static readonly string AtsPPower = nameof(AtsPPower).ToCamelCase();

        /// <summary>
        ///     パターン接近
        /// </summary>
        public static readonly string AtsPPatternApproach = nameof(AtsPPatternApproach).ToCamelCase();

        /// <summary>
        ///     常用ブレーキ
        /// </summary>
        public static readonly string AtsPServiceBrake = nameof(AtsPServiceBrake).ToCamelCase();

        /// <summary>
        ///     非常ブレーキ
        /// </summary>
        public static readonly string AtsPEmergencyBrake = nameof(AtsPEmergencyBrake).ToCamelCase();

        /// <summary>
        ///     ブレーキ開放
        /// </summary>
        public static readonly string AtsPBrakeCutout = nameof(AtsPBrakeCutout).ToCamelCase();

        /// <summary>
        ///     ATS-P
        /// </summary>
        public static readonly string AtsPEnabled = nameof(AtsPEnabled).ToCamelCase();

        /// <summary>
        ///     故障
        /// </summary>
        public static readonly string AtsPFailure = nameof(AtsPFailure).ToCamelCase();

        public static readonly IReadOnlyList<string> AtsPLampIds = new List<string>
        {
            AtsPPower, AtsPPatternApproach, AtsPServiceBrake, AtsPEmergencyBrake, AtsPBrakeCutout, AtsPEnabled,
            AtsPFailure
        };

        /// <summary>
        ///     抑速
        /// </summary>
        public static readonly string HoldSpeed = nameof(HoldSpeed).ToCamelCase();

        #region ATS-S

        /// <summary>
        ///     ATS電源
        /// </summary>
        public static readonly string AtsSPower = nameof(AtsSPower).ToCamelCase();

        /// <summary>
        ///     ATS動作
        /// </summary>
        public static readonly string AtsSActivated = nameof(AtsSActivated).ToCamelCase();

        public static readonly IReadOnlyList<string> AtsSLampIds = new List<string>
        {
            AtsSPower, AtsSActivated
        };

        #endregion


        #region VehicleState

        // TODO 此灯功能未知
        public static readonly string Acc = nameof(Acc).ToCamelCase();

        /// <summary>
        ///     三相
        /// </summary>
        public static readonly string ThreePhase = nameof(ThreePhase).ToCamelCase();

        /// <summary>
        ///     非常短絡
        /// </summary>
        public static readonly string EmergencyShunt = nameof(EmergencyShunt).ToCamelCase();

        /// <summary>
        ///     耐雪ブレーキ
        /// </summary>
        public static readonly string SnowBrake = nameof(SnowBrake).ToCamelCase();

        /// <summary>
        ///     直通予備
        /// </summary>
        public static readonly string DirectAirBackupBrake = nameof(DirectAirBackupBrake).ToCamelCase();

        /// <summary>
        ///     定速
        /// </summary>
        public static readonly string ConstantSpeed = nameof(ConstantSpeed).ToCamelCase();

        /// <summary>
        ///     驻車ブレーキ
        /// </summary>
        public static readonly string SpringBrake = nameof(SpringBrake).ToCamelCase();

        #endregion

        #region ATC

        public static readonly string AtcSpeed0 = nameof(AtcSpeed0).ToCamelCase();
        public static readonly string AtcSpeed15 = nameof(AtcSpeed15).ToCamelCase();
        public static readonly string AtcSpeed25 = nameof(AtcSpeed25).ToCamelCase();
        public static readonly string AtcSpeed45 = nameof(AtcSpeed45).ToCamelCase();
        public static readonly string AtcSpeed55 = nameof(AtcSpeed55).ToCamelCase();
        public static readonly string AtcSpeed65 = nameof(AtcSpeed65).ToCamelCase();
        public static readonly string AtcSpeed75 = nameof(AtcSpeed75).ToCamelCase();
        public static readonly string AtcSpeed90 = nameof(AtcSpeed90).ToCamelCase();
        public static readonly string AtcSpeed100 = nameof(AtcSpeed100).ToCamelCase();
        public static readonly string AtcSpeed110 = nameof(AtcSpeed110).ToCamelCase();
        public static readonly string AtcSpeed120 = nameof(AtcSpeed120).ToCamelCase();

        public static readonly IReadOnlyList<string> IntermittentAtcLampIds =
            new List<string>
            {
                AtcSpeed0, AtcSpeed15, AtcSpeed25, AtcSpeed45, AtcSpeed55, AtcSpeed65, AtcSpeed75, AtcSpeed90,
                AtcSpeed100, AtcSpeed110, AtcSpeed120
            };

        public static readonly string Atc6Shunt = nameof(Atc6Shunt).ToCamelCase();
        public static readonly string Atc6AbsoluteStop = nameof(Atc6AbsoluteStop).ToCamelCase();
        public static readonly string Atc6PatternApproach = nameof(Atc6PatternApproach).ToCamelCase();
        public static readonly string Atc6InchingActivated = nameof(Atc6InchingActivated).ToCamelCase();
        public static readonly string Atc6TurnOff = nameof(Atc6TurnOff).ToCamelCase();
        public static readonly string Atc6PatternCleared = nameof(Atc6PatternCleared).ToCamelCase();
        public static readonly string Atc6EmergencyRun = nameof(Atc6EmergencyRun).ToCamelCase();
        public static readonly string Atc6ServiceBrake = nameof(Atc6ServiceBrake).ToCamelCase();
        public static readonly string Atc6EmergencyBrake = nameof(Atc6EmergencyBrake).ToCamelCase();
        public static readonly string Atc6OverrunAction = nameof(Atc6OverrunAction).ToCamelCase();
        public static readonly string Atc6Power = nameof(Atc6Power).ToCamelCase();
        public static readonly string Atc6Cutout = nameof(Atc6Cutout).ToCamelCase();

        public static readonly IReadOnlyList<string> Atc6LampIds = new List<string>
        {
            Atc6Shunt, Atc6AbsoluteStop, Atc6PatternApproach, Atc6InchingActivated, Atc6TurnOff, Atc6PatternCleared,
            Atc6EmergencyRun, Atc6ServiceBrake, Atc6EmergencyBrake, Atc6OverrunAction, Atc6Power, Atc6Cutout
        };

        public static readonly string DatcSpeedLimit = nameof(DatcSpeedLimit).ToCamelCase();
        public static readonly string DatcShunt = nameof(DatcShunt).ToCamelCase();
        public static readonly string DatcAbsoluteStop = nameof(DatcAbsoluteStop).ToCamelCase();
        public static readonly string DatcPatternApproach = nameof(DatcPatternApproach).ToCamelCase();
        public static readonly string DatcInchingActivated = nameof(DatcInchingActivated).ToCamelCase();
        public static readonly string DatcTurnOff = nameof(DatcTurnOff).ToCamelCase();
        public static readonly string DatcPatternCleared = nameof(DatcPatternCleared).ToCamelCase();
        public static readonly string DatcEmergencyRun = nameof(DatcEmergencyRun).ToCamelCase();
        public static readonly string DatcServiceBrake = nameof(DatcServiceBrake).ToCamelCase();
        public static readonly string DatcEmergencyBrake = nameof(DatcEmergencyBrake).ToCamelCase();
        public static readonly string DatcOverrunAction = nameof(DatcOverrunAction).ToCamelCase();
        public static readonly string DatcPower = nameof(DatcPower).ToCamelCase();
        public static readonly string DatcCutout = nameof(DatcCutout).ToCamelCase();

        /// <summary>
        ///     転動防止動作
        /// </summary>
        public static readonly string AtcHoldingBrakeActive = nameof(AtcHoldingBrakeActive).ToCamelCase();

        public static readonly IReadOnlyList<string> DatcLampIds = new List<string>
        {
            DatcShunt, DatcAbsoluteStop, DatcPatternApproach, DatcInchingActivated, DatcTurnOff, DatcPatternCleared,
            DatcEmergencyRun, DatcServiceBrake, DatcEmergencyBrake, DatcOverrunAction, DatcPower, DatcCutout,
            AtcHoldingBrakeActive
        };

        #endregion

        #region TASC

        /// <summary>
        ///     TASC制动级位
        /// </summary>
        public static readonly string TascBrakeNotch = nameof(TascBrakeNotch).ToCamelCase();

        /// <summary>
        ///     TASC電源
        /// </summary>
        public static readonly string TascPower = nameof(TascPower).ToCamelCase();

        /// <summary>
        ///     TASCパターン
        /// </summary>
        public static readonly string TascPattern = nameof(TascPattern).ToCamelCase();

        /// <summary>
        ///     TASCブレーキ
        /// </summary>
        public static readonly string TascBrake = nameof(TascBrake).ToCamelCase();

        public static readonly string TascEnabled = nameof(TascEnabled).ToCamelCase();

        public static readonly string TascDisabled = nameof(TascDisabled).ToCamelCase();

        /// <summary>
        ///     TASC故障
        /// </summary>
        public static readonly string TascFailure = nameof(TascFailure).ToCamelCase();

        /// <summary>
        ///     定位置
        /// </summary>
        public static readonly string TascFixedDistance = nameof(TascFixedDistance).ToCamelCase();

        /// <summary>
        ///     車両ドア全閉
        /// </summary>
        public static readonly string VehicleDoorAllClosed = nameof(VehicleDoorAllClosed).ToCamelCase();

        /// <summary>
        ///     ホームドア全閉
        /// </summary>
        public static readonly string PlatformDoorAllClosed = nameof(PlatformDoorAllClosed).ToCamelCase();

        /// <summary>
        ///     ホームドア連携
        /// </summary>
        public static readonly string PlatformInterlocking = nameof(PlatformInterlocking).ToCamelCase();

        /// <summary>
        ///     ホームドア分離
        /// </summary>
        public static readonly string PlatformDecoupling = nameof(PlatformDecoupling).ToCamelCase();


        public static readonly IReadOnlyList<string> TascBaseLampIds = new List<string>
        {
            TascBrakeNotch, TascPower, TascPattern, TascBrake, TascEnabled, TascDisabled, TascFailure,
            TascFixedDistance, VehicleDoorAllClosed, PlatformDoorAllClosed, PlatformInterlocking, PlatformDecoupling
        };

        /// <summary>
        ///     転動防止ブレーキ
        /// </summary>
        public static readonly string TascHoldingBrake = nameof(TascHoldingBrake).ToCamelCase();

        /// <summary>
        ///     ホームドア開放
        /// </summary>
        public static readonly string PlatformDoorCutout = nameof(PlatformDoorCutout).ToCamelCase();

        public static readonly IReadOnlyList<string> TascExtraLampIds = new List<string>
            { TascHoldingBrake, PlatformDoorCutout };

        #endregion
    }
}