using System;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.ViewModels
{
    public class ButtonViewModel : ViewModel
    {
        private const double PressedDelayMs = 80;
        private readonly bool _playPressAnimation;
        private readonly bool _reboundImmediate;
        private double _elapsedRecessedTime;
        private MonitorSoundController _monitorSoundController;
        private bool _shouldResetPressed;
        private TickTracker _tickTracker;

        public ButtonViewModel(bool clickable, bool reboundImmediate, bool playPressAnimation = true)
        {
            Pressed = new Signal<bool>();
            Clickable = CreatePropertySlot(clickable);
            _reboundImmediate = reboundImmediate;
            _playPressAnimation = playPressAnimation;
        }

        public PropertySlot<bool> Clickable { get; }
        public Signal<bool> Pressed { get; }
        public event Action OnClick;

        protected override void OnInitialize(DataHub dataHub)
        {
            var delayService = dataHub.Get<DelayService>();
            _tickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Button));
            _monitorSoundController = dataHub.GetOrNull<MonitorSoundController>();
        }

        public void TryClick()
        {
            if (!Clickable || Pressed.Value) return;
            _tickTracker.TryRequestNextTick();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_shouldResetPressed)
            {
                _shouldResetPressed = false;
                Pressed.Value = false;
            }

            if (_tickTracker.ConsumeNextTick())
            {
                _monitorSoundController?.PlaySound(SoundIds.ButtonClick);
                if (_playPressAnimation)
                {
                    Pressed.Value = true;
                    _elapsedRecessedTime = 0;
                }
                else
                {
                    OnClick?.Invoke();
                }
            }

            if (_playPressAnimation && Pressed.Value)
            {
                _elapsedRecessedTime += elapsed.TotalMilliseconds;
                if (_elapsedRecessedTime >= PressedDelayMs)
                {
                    OnClick?.Invoke();
                    if (_reboundImmediate)
                        Pressed.Value = false;
                    else
                        _shouldResetPressed = true;
                }
            }
        }

        protected override void OnReset()
        {
            _tickTracker.Reset();
            Pressed.Value = false;
            _shouldResetPressed = false;
        }
    }
}