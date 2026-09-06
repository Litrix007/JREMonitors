using System.Collections.Generic;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Monitors;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.State;

namespace JREMonitors.BveEx.Builders.Base
{
    public interface IVehicleBuilder
    {
        void PopulateRootDataHub(VehicleBuildContext context, VehicleConfig config);

        void PopulateMonitorLocalDataHub(VehicleBuildContext context, Dictionary<string, Monitor> monitors,
            VehicleConfig config);

        void PostPopulateRootDataHub(VehicleBuildContext context, VehicleConfig config);

        IEnumerable<MonitorProperties> CreateMonitorProperties(
            MonitorContext context,
            VehicleConfig config, DataHub dataHub);

        void Reconfigure(VehicleBuildContext context, VehicleConfig oldConfig, VehicleConfig newConfig);

        void ReconfigureMonitorLocal(VehicleBuildContext context, VehicleConfig oldConfig, VehicleConfig newConfig,
            IList<Monitor> monitors);

        void OnMonitorsRemoved(VehicleBuildContext context, IList<Monitor> removedMonitors,
            VehicleConfig config);
    }
}