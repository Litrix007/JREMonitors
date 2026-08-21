using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.ViewModels
{
    public class VehicleStateLampGroupViewModel : ViewModel
    {
        private TickTracker _normalTickTracker;
        private IPanelDataProvider _panelDataProvider;
        public Signal<bool> IsAccLit { get; } = new Signal<bool>();
        public Signal<bool> IsThreePhaseLit { get; } = new Signal<bool>();
        public Signal<bool> IsEmergencyShuntLit { get; } = new Signal<bool>();
        public Signal<bool> IsSnowBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsDirectAirBackupBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsConstantSpeedLit { get; } = new Signal<bool>();
        public Signal<bool> IsSpringBrakeLit { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_normalTickTracker.TrackAndSync())
            {
                IsAccLit.Value = _panelDataProvider.IsActive(DirectInputIds.Acc);
                IsThreePhaseLit.Value = _panelDataProvider.IsActive(DirectInputIds.ThreePhase);
                IsEmergencyShuntLit.Value = _panelDataProvider.IsActive(DirectInputIds.EmergencyShunt);
                IsSnowBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.SnowBrake);
                IsDirectAirBackupBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.DirectAirBackupBrake);
                IsConstantSpeedLit.Value = _panelDataProvider.IsActive(DirectInputIds.ConstantSpeed);
                IsSpringBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.SpringBrake);
            }
        }

        protected override void OnReset()
        {
            _normalTickTracker.Reset();
        }
    }
}