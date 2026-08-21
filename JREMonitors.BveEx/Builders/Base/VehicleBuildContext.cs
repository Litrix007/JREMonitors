using JREMonitors.BveEx.Providers;
using JREMonitors.Core.Managers;
using JREMonitors.Core.State;

namespace JREMonitors.BveEx.Builders.Base
{
    public class VehicleBuildContext
    {
        public VehicleBuildContext(DataHub rootDataHub, TickUpdateManager tickUpdateManager,
            JumpStationManager jumpStationManager)
        {
            RootDataHub = rootDataHub;
            TickUpdateManager = tickUpdateManager;
            JumpStationManager = jumpStationManager;
        }

        public DataHub RootDataHub { get; }
        public TickUpdateManager TickUpdateManager { get; }
        public JumpStationManager JumpStationManager { get; }
    }
}