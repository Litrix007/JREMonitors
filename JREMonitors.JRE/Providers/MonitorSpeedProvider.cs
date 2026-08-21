using System;
using JREMonitors.Core.Providers;

namespace JREMonitors.JRE.Providers
{
    public class MonitorSpeedProvider : ITickUpdatable
    {
        private readonly IDelayProvider _speedDelayProvider;
        private readonly IVehicleStateProvider _vehicleStateProvider;
        private long _speedTickCount = -1;

        public MonitorSpeedProvider(IVehicleStateProvider vehicleStateProvider, IDelayProvider speedDelayProvider)
        {
            _vehicleStateProvider = vehicleStateProvider;
            _speedDelayProvider = speedDelayProvider;
        }

        public int Speed { get; private set; }

        public void Update(TimeSpan elapsed)
        {
            if (_speedDelayProvider.TickCount == _speedTickCount) return;
            _speedTickCount = _speedDelayProvider.TickCount;
            Speed = (int)Math.Abs(_vehicleStateProvider.Speed);
        }
    }
}