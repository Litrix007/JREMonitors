using System;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.ViewModels
{
    public class MeterBackgroundBaseViewModel : ViewModel
    {
        public readonly Signal<bool> IsSafetyLampVisible = new Signal<bool>();
        private E233MonitorStateController _monitorStateController;

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorStateController = dataHub.GetOrNull<E233MonitorStateController>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            IsSafetyLampVisible.Value = _monitorStateController?.IsSafetyLampVisibleOnMeterScreen ?? true;
        }
    }
}