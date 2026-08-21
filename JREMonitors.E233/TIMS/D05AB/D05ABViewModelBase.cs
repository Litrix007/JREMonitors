using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.E233.TIMS.D05AB
{
    public abstract class D05ABViewModelBase : TIMSFormationViewModel
    {
        private readonly string _carStateDelayType;
        private CarStateService _carStateService;
        private TickTracker _tickTracker;

        protected D05ABViewModelBase(TIMSVehicleSpec spec, string carStateDelayType) : base(spec)
        {
            _carStateDelayType = carStateDelayType;
        }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _carStateService = dataHub.Get<CarStateService>();
            var delayService = dataHub.Get<DelayService>();
            _tickTracker = new TickTracker(delayService.GetDelayProvider(_carStateDelayType));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (!_tickTracker.TrackAndSync()) return;
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
            _tickTracker.Reset();
        }
    }
}