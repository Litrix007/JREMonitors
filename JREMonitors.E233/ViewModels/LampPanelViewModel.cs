using System;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.ViewModels
{
    public class LampPanelViewModel : ViewModel
    {
        private MonitorSoundController _monitorSoundController;
        private TickTracker _tickTracker;

        public LampPanelViewModel(bool clickable)
        {
            Clickable = clickable;
        }

        public bool Clickable { get; set; }
        public event Action OnClick;

        protected override void OnInitialize(DataHub dataHub)
        {
            var delayService = dataHub.Get<DelayService>();
            _tickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Button));
            _monitorSoundController = dataHub.GetOrNull<MonitorSoundController>();
        }

        public void TryClick()
        {
            if (!Clickable) return;
            _tickTracker.TryRequestNextTick();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_tickTracker.ConsumeNextTick())
            {
                OnClick?.Invoke();
                _monitorSoundController?.PlaySound(SoundIds.ButtonClick);
            }
        }

        protected override void OnReset()
        {
            _tickTracker.Reset();
        }
    }
}