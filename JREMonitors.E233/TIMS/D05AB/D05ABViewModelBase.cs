using System;
using System.Collections.Generic;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.E233.TIMS.D05AB
{
    public abstract class D05ABViewModelBase : TIMSFormationViewModel
    {
        private CarStateService _carStateService;
        protected readonly List<TickTracker> TickTrackers = new List<TickTracker>();

        protected D05ABViewModelBase(TIMSVehicleSpec spec) : base(spec)
        {
        }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _carStateService = dataHub.Get<CarStateService>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (IsVehicleDirectionChanged || IsFormationSpecChanged)
            {
                for (var i = 0; i < TickTrackers.Count; i++)
                {
                    var tickTracker = TickTrackers[i];
                    tickTracker.Reset();
                }
            }

            if (FormationSpec.Value == null) return;
            var carCount = FormationSpec.Value.CarCount;
            var vehicleDirection = VehicleDirection.Value;
            for (var i = 0; i < carCount; i++)
            {
                var dataIdx = TIMSVehicleSpec.GetDataIndex(carCount, vehicleDirection, i);
                var carState = _carStateService.GetCarStateAt(dataIdx);
                if (carState == null) continue;
                OnUpdateCarState(i, carState);
            }
        }

        protected virtual void OnUpdateCarState(int i, ICarState carState)
        {
        }

        protected override void OnReset()
        {
            base.OnReset();
            for (var i = 0; i < TickTrackers.Count; i++)
            {
                var tickTracker = TickTrackers[i];
                tickTracker.Reset();
            }
        }
    }
}