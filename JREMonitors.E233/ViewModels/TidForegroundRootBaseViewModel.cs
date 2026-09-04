using System;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.ViewModels
{
    public class TidForegroundRootBaseViewModel : ViewModel
    {
        public readonly Signal<bool> SupportsTasc = new Signal<bool>();
        private E233MonitorStates _monitorStates;

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            SupportsTasc.Value = _monitorStates.SupportsTasc;
        }
    }
}