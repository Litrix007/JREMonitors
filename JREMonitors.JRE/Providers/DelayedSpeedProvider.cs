using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.JRE.Constants;

namespace JREMonitors.JRE.Providers
{
    public class DelayedSpeedProvider : ITickUpdatable
    {
        private readonly IDelayProvider _speedDelayProvider;
        private readonly IVehicleStateProvider _vehicleStateProvider;
        private long _speedTickCount = -1;

        public DelayedSpeedProvider(IVehicleStateProvider vehicleStateProvider, DelayService delayService)
        {
            _vehicleStateProvider = vehicleStateProvider;
            _speedDelayProvider = delayService.GetDelayProvider(DelayTypes.Speed);
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