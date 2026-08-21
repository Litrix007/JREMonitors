using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.ViewModels
{
    public class AtsStateViewModel : ViewModel
    {
        private IDelayProvider _normalDelayProvider;
        private long _normalTickCount = -1;
        private IPanelDataProvider _panelDataProvider;

        public Signal<bool> IsAtsPPowerLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPPatternApproachLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPServiceBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPEmergencyBrakeLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPBrakeCutoutLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPEnabledLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsPFailureLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsSPowerLit { get; } = new Signal<bool>();
        public Signal<bool> IsAtsSActivatedLit { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalDelayProvider = delayService.GetDelayProvider(DelayTypes.Normal);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_normalDelayProvider.TickCount == _normalTickCount) return;
            _normalTickCount = _normalDelayProvider.TickCount;
            IsAtsPPowerLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPPower);
            IsAtsPPatternApproachLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPPatternApproach);
            IsAtsPServiceBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPServiceBrake);
            IsAtsPEmergencyBrakeLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPEmergencyBrake);
            IsAtsPBrakeCutoutLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPBrakeCutout);
            IsAtsPEnabledLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPEnabled);
            IsAtsPFailureLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsPFailure);
            IsAtsSPowerLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsSPower);
            IsAtsSActivatedLit.Value = _panelDataProvider.IsActive(DirectInputIds.AtsSActivated);
        }

        protected override void OnReset()
        {
            _normalTickCount = -1;
        }
    }
}