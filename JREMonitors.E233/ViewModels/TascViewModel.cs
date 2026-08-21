using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.ViewModels
{
    public class TascViewModel : ViewModel
    {
        private IDelayProvider _normalDelayProvider;
        private long _normalTickCount = -1;
        private IPanelDataProvider _panelDataProvider;
        public Signal<bool> IsInchingActivatedLit { get; } = new Signal<bool>();
        public Signal<bool> IsPlatformDecouplingLit { get; } = new Signal<bool>();
        public Signal<bool> IsPlatformDoorAllClosedLit { get; } = new Signal<bool>();
        public Signal<bool> IsPlatformDoorCutoutLit { get; } = new Signal<bool>();
        public Signal<bool> IsPlatformInterlockingLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascFailureLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascFixedDistanceLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascHoldingBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascPatternLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascPowerLit { get; } = new Signal<bool>();
        public Signal<bool> IsTascTurnOffLit { get; } = new Signal<bool>();
        public Signal<bool> IsVehicleDoorAllClosedLit { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalDelayProvider = delayService.GetDelayProvider(DelayTypes.Normal);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_normalDelayProvider.TickCount == _normalTickCount) return;
            IsInchingActivatedLit.Value = _panelDataProvider.IsActive(LogicalInputIds.InchingActivated);
            _normalTickCount = _normalDelayProvider.TickCount;
            IsTascPowerLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascPower);
            IsTascPatternLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascPattern);
            IsTascBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascBrake);
            IsTascTurnOffLit.Value = _panelDataProvider.IsActive(LogicalInputIds.TascTurnOff);
            IsTascFailureLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascFailure);
            IsTascFixedDistanceLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascFixedDistance);
            IsVehicleDoorAllClosedLit.Value = _panelDataProvider.IsActive(DirectInputIds.VehicleDoorAllClosed);
            IsPlatformDoorAllClosedLit.Value = _panelDataProvider.IsActive(DirectInputIds.PlatformDoorAllClosed);
            IsPlatformInterlockingLit.Value = _panelDataProvider.IsActive(DirectInputIds.PlatformInterlocking);
            IsPlatformDecouplingLit.Value = _panelDataProvider.IsActive(DirectInputIds.PlatformDecoupling);
            IsTascHoldingBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.TascHoldingBrake);
            IsPlatformDoorCutoutLit.Value = _panelDataProvider.IsActive(DirectInputIds.PlatformDoorCutout);
        }

        protected override void OnReset()
        {
            _normalTickCount = -1;
        }
    }
}