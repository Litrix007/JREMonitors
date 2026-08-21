using System;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Providers;

namespace JREMonitors.E233.TIMS
{
    public class TIMSViewModel : ViewModel
    {
        protected TIMSICCardService<E233SignalSystem> ICCardService;
        protected TIMSService TIMSService;
        public Signal<TIMSVehicleDirection> VehicleDirection { get; } = new Signal<TIMSVehicleDirection>();
        protected bool IsVehicleDirectionChanged { get; private set; } = true;

        protected override void OnInitialize(DataHub dataHub)
        {
            TIMSService = dataHub.Get<TIMSService>();
            ICCardService = dataHub.Get<TIMSICCardService<E233SignalSystem>>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (Signal<TIMSVehicleDirection>.IsValueChanged(VehicleDirection, TIMSService.VehicleDirection))
            {
                IsVehicleDirectionChanged = true;
                VehicleDirection.Value = TIMSService.VehicleDirection;
            }
            else
            {
                IsVehicleDirectionChanged = false;
            }
        }

        protected override void OnReset()
        {
            IsVehicleDirectionChanged = true;
        }
    }
}