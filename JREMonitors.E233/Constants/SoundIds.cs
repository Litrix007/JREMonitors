using System.Collections.Generic;
using JREMonitors.Core.Utils;

namespace JREMonitors.E233.Constants
{
    public static class SoundIds
    {
        public static readonly string ButtonClick = nameof(ButtonClick).ToCamelCase();
        public static readonly string ArrivalHint = nameof(ArrivalHint).ToCamelCase();
        public static readonly string PassHint = nameof(PassHint).ToCamelCase();
        public static readonly string AirSectionHint = nameof(AirSectionHint).ToCamelCase();
        public static readonly string RadioChannelChange = nameof(RadioChannelChange).ToCamelCase();

        public static readonly HashSet<string> All = new HashSet<string>
        {
            ButtonClick, ArrivalHint, PassHint, AirSectionHint, RadioChannelChange
        };
    }
}