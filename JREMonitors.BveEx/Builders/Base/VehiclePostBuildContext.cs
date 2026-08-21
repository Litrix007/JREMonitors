using JREMonitors.BveEx.Providers;
using JREMonitors.Core.Managers;
using JREMonitors.Core.State;

namespace JREMonitors.BveEx.Builders.Base
{
    public class VehiclePostBuildContext : VehicleBuildContext
    {
        public VehiclePostBuildContext(DataHub rootDataHub, TickUpdateManager tickUpdateManager,
            JumpStationManager jumpStationManager) : base(rootDataHub,
            tickUpdateManager, jumpStationManager)
        {
        }
    }
}