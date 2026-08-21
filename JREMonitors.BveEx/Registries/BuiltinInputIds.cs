using System.Collections.Generic;
using JREMonitors.Core.Utils;

namespace JREMonitors.BveEx.Registries
{
    public static class BuiltinInputIds
    {
        /// <summary>
        ///     制动级位
        /// </summary>
        public static readonly string BrakeNotch = nameof(BrakeNotch).ToCamelCase();

        public static readonly HashSet<string> All = new HashSet<string> { BrakeNotch };
    }
}