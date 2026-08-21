using System.Collections.Generic;
using JREMonitors.Core.Utils;

namespace JREMonitors.E233.Constants
{
    public static class SoundIds
    {
        public static readonly string ButtonClick = nameof(ButtonClick).ToCamelCase();
        public static readonly HashSet<string> All = new HashSet<string> { ButtonClick };
    }
}