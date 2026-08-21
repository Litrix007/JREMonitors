using System;

namespace JREMonitors.Core.Utils
{
    public static class TimeSpanHelper
    {
        public static TimeSpan MaxZero(this TimeSpan ts)
        {
            return ts > TimeSpan.Zero ? ts : TimeSpan.Zero;
        }

        public static TimeSpan? MaxZero(this TimeSpan? ts)
        {
            if (!ts.HasValue) return null;
            return ts.Value > TimeSpan.Zero ? ts.Value : TimeSpan.Zero;
        }
    }
}