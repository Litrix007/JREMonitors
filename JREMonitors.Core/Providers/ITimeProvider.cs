using System;

namespace JREMonitors.Core.Providers
{
    public interface ITimeProvider
    {
        TimeSpan CurrentTime { get; }
    }
}