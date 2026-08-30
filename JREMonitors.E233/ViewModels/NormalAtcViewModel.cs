using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;

namespace JREMonitors.E233.ViewModels
{
    public class NormalAtcViewModel : ViewModel
    {
        private TickTracker _normalTickTracker;
        private IPanelDataProvider _panelDataProvider;
        private ISignalProvider<E233SignalSystem> _signalProvider;
        public Signal<bool> IsAtc6EnabledLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcCutoutLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcEmergencyBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcPowerLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcServiceBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcTurnOffLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsSActivatedLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsSPowerLit { get; } = new Signal<bool>();
        public Signal<bool> IsDatcEnabledLit { get; } = new Signal<bool>();
        public Signal<bool> IsEmergencyRunLit { get; } = new Signal<bool>();
        public Signal<bool> IsInchingActivatedLit { get; } = new Signal<bool>();
        public Signal<bool> IsOverrunActionLit { get; } = new Signal<bool>();
        public Signal<bool> IsPatternClearedLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtcHoldingBrakeActive { get; } = new Signal<bool>();


        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            _signalProvider = dataHub.Get<ISignalProvider<E233SignalSystem>>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_normalTickTracker.TrackAndSync())
            {
                IsInchingActivatedLit.Value = _panelDataProvider.IsActive(LogicalInputIds.InchingActivated);
                var activeSignalSystem = _signalProvider.ActiveSignalSystem;
                IsAtcTurnOffLit.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcTurnOff);
                IsAtsSPowerLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsSPower);
                IsPatternClearedLit.Value = _panelDataProvider.IsActive(LogicalInputIds.PatternCleared);
                IsEmergencyRunLit.Value = _panelDataProvider.IsActive(LogicalInputIds.EmergencyRun);
                IsAtcServiceBrakeLit.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcServiceBrake);
                IsAtcEmergencyBrakeLit.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcEmergencyBrake);
                IsOverrunActionLit.Value = _panelDataProvider.IsActive(LogicalInputIds.OverrunAction);
                IsAtsSActivatedLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsSActivated);
                IsAtcPowerLit.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcPower);
                IsDatcEnabledLit.Value =
                    !IsAtcTurnOffLit && activeSignalSystem == E233SignalSystem.Datc && IsAtcPowerLit;
                IsAtc6EnabledLit.Value =
                    !IsAtcTurnOffLit && activeSignalSystem == E233SignalSystem.Atc6 && IsAtcPowerLit;
                IsAtcCutoutLit.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcCutout);
                IsAtcHoldingBrakeActive.Value = _panelDataProvider.IsActive(DirectInputIds.AtcHoldingBrakeActive);
            }
        }

        protected override void OnReset()
        {
            _normalTickTracker.Reset();
        }
    }
}