using System;
using JREMonitors.Core.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BveTimeProvider : ITimeProvider
    {
        public TimeSpan CurrentTime { get; set; }
    }
}