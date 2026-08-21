using System;

namespace JREMonitors.Core.Providers
{
    public interface IDelayProvider
    {
        long TickCount { get; }
        void Update(TimeSpan elapsed);
    }
}