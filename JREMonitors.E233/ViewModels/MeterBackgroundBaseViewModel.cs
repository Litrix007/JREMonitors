using System;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.ViewModels
{
    public class MeterBackgroundBaseViewModel : ViewModel
    {
        public readonly Signal<bool> IsSafetyLampVisible = new Signal<bool>();
        private E233MonitorStates _monitorStates;

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorStates = dataHub.GetOrNull<E233MonitorStates>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            IsSafetyLampVisible.Value = _monitorStates?.IsSafetyLampVisibleOnMeterScreen ?? true;
        }
    }
}