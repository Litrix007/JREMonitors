using System;

namespace JREMonitors.Core.Providers
{
    public interface ITickUpdatable
    {
        void Update(TimeSpan elapsed);
    }
}