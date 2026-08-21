using System.Collections.Generic;
using JREMonitors.JRE.Constants;

namespace JREMonitors.BveEx.Registries
{
    public static class BuiltinInputPresetRegistry
    {
        public const string GapAtsP = "GAP-ATS-P";
        public const string GapAtsS = "GAP-ATS-S";
        public const string Atc6 = "ATC-6";
        public const string Mi5000Datc = "Mi5000-DATC";

        public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<int>>>
            InputPresets =
                new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<int>>>
                {
                    {
                        GapAtsP, new Dictionary<string, IReadOnlyList<int>>
                        {
                            { DirectInputIds.AtsPPower, new[] { 2 } },
                            { DirectInputIds.AtsPPatternApproach, new[] { 3 } },
                            { DirectInputIds.AtsPBrakeCutout, new[] { 4 } },
                            { DirectInputIds.AtsPServiceBrake, new[] { 5 } },
                            { DirectInputIds.AtsPEnabled, new[] { 6 } },
                            { DirectInputIds.AtsPFailure, new[] { 7 } },
                            { DirectInputIds.AtsPEmergencyBrake, new[] { 8 } }
                        }
                    },
                    {
                        GapAtsS, new Dictionary<string, IReadOnlyList<int>>
                        {
                            { DirectInputIds.AtsSPower, new[] { 0 } },
                            { DirectInputIds.AtsSActivated, new[] { 1 } }
                        }
                    },
                    {
                        Atc6, new Dictionary<string, IReadOnlyList<int>>
                        {
                            { DirectInputIds.AtcSpeed0, new[] { 71 } },
                            { DirectInputIds.AtcSpeed15, new[] { 72 } },
                            { DirectInputIds.AtcSpeed25, new[] { 73 } },
                            { DirectInputIds.AtcSpeed45, new[] { 74 } },
                            { DirectInputIds.AtcSpeed55, new[] { 75 } },
                            { DirectInputIds.AtcSpeed65, new[] { 76 } },
                            { DirectInputIds.AtcSpeed75, new[] { 77 } },
                            { DirectInputIds.AtcSpeed90, new[] { 78 } },
                            { DirectInputIds.AtcSpeed100, new[] { 79 } },
                            { DirectInputIds.AtcSpeed110, new[] { 80 } },
                            { DirectInputIds.AtcSpeed120, new[] { 81 } },
                            { DirectInputIds.Atc6PatternApproach, new[] { 68 } },
                            { DirectInputIds.Atc6Shunt, new[] { 69 } },
                            { DirectInputIds.Atc6AbsoluteStop, new[] { 70 } },
                            { DirectInputIds.Atc6ServiceBrake, new[] { 58 } },
                            { DirectInputIds.Atc6EmergencyBrake, new[] { 57 } },
                            { DirectInputIds.Atc6Power, new[] { 52 } },
                            { DirectInputIds.Atc6TurnOff, new[] { 54 } }
                        }
                    },
                    {
                        Mi5000Datc, new Dictionary<string, IReadOnlyList<int>>
                        {
                            { DirectInputIds.DatcSpeedLimit, new[] { 67 } },
                            { DirectInputIds.DatcPatternApproach, new[] { 68 } },
                            { DirectInputIds.DatcAbsoluteStop, new[] { 70 } },
                            { DirectInputIds.DatcServiceBrake, new[] { 58, 65 } },
                            { DirectInputIds.DatcEmergencyBrake, new[] { 57 } },
                            { DirectInputIds.DatcPower, new[] { 52 } },
                            { DirectInputIds.DatcTurnOff, new[] { 54 } }
                        }
                    }
                };
    }
}