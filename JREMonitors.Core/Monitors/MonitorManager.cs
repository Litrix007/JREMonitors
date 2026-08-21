using JREMonitors.Core.Contexts;
using JREMonitors.Core.State;

namespace JREMonitors.Core.Monitors
{
    public abstract class MonitorManager
    {
        protected readonly MonitorContext Context;
        protected readonly DataHub DataHub;

        protected MonitorManager(MonitorContext context, DataHub dataHub)
        {
            Context = context;
            DataHub = dataHub;
        }
    }
}