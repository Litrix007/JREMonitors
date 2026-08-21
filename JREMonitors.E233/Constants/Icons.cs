using JREMonitors.Core.Utils;

namespace JREMonitors.E233.Constants
{
    public static class Icons
    {
        public static readonly string Brightness =
            ResourceHelper.GetResourceString(typeof(Icons), $"JREMonitors.E233.Resources.{nameof(Brightness)}.svg");

        public static readonly string Home =
            ResourceHelper.GetResourceString(typeof(Icons), $"JREMonitors.E233.Resources.{nameof(Home)}.svg");
    }
}