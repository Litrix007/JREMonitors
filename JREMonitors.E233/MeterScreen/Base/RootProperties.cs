using System.Numerics;

namespace JREMonitors.E233.MeterScreen.Base
{
    public readonly struct RootProperties
    {
        public readonly bool ShowHoldSpeedLamp;
        public readonly Vector2 DeviceVoltageGaugePos;
        public readonly float DeviceVoltageSectorRadius;
        public readonly Vector2 CatenaryVoltageGaugePos;
        public readonly float CatenaryVoltageSectorRadius;
        public readonly Vector2 CurrentGaugePos;
        public readonly float CurrentSectorRadius;
        public readonly float SpeedOffsetY;

        public RootProperties(
            bool showHoldSpeedLamp,
            Vector2 deviceVoltageGaugePos,
            float deviceVoltageSectorRadius,
            Vector2 catenaryVoltageGaugePos,
            float catenaryVoltageSectorRadius,
            Vector2 currentGaugePos,
            float currentSectorRadius,
            float speedOffsetY
        )
        {
            ShowHoldSpeedLamp = showHoldSpeedLamp;
            DeviceVoltageGaugePos = deviceVoltageGaugePos;
            DeviceVoltageSectorRadius = deviceVoltageSectorRadius;
            CatenaryVoltageGaugePos = catenaryVoltageGaugePos;
            CatenaryVoltageSectorRadius = catenaryVoltageSectorRadius;
            CurrentGaugePos = currentGaugePos;
            CurrentSectorRadius = currentSectorRadius;
            SpeedOffsetY = speedOffsetY;
        }
    }
}