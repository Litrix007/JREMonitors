using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;

namespace JREMonitors.E233.ViewModels
{
    public class AtcSpeedViewModel : ViewModel
    {
        private DelayedSpeedProvider _delayedSpeedProvider;
        private TickTracker _normalTickTracker;
        private IPanelDataProvider _panelDataProvider;
        private bool _powerOff;
        private ISignalProvider<E233SignalSystem> _signalProvider;
        private TickTracker _speedTickTracker;

        public Signal<bool> TurnOff { get; } = new Signal<bool>();
        public Signal<bool> ShowAtcParts { get; } = new Signal<bool>();
        public Signal<int> Speed { get; } = new Signal<int>();
        public Signal<int> SpeedLimit { get; } = new Signal<int>();
        public Signal<bool> Shunt { get; } = new Signal<bool>();
        public Signal<bool> AbsoluteStop { get; } = new Signal<bool>();
        public Signal<bool> PatternApproach { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            _signalProvider = dataHub.Get<ISignalProvider<E233SignalSystem>>();
            _delayedSpeedProvider = dataHub.Get<DelayedSpeedProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
            _speedTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Speed));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            var activeSignalSystem = _signalProvider.ActiveSignalSystem;
            _powerOff = !_panelDataProvider.IsActive(LogicalInputIds.AtcPower);
            if (_normalTickTracker.TrackAndSync())
            {
                ShowAtcParts.Value = activeSignalSystem == E233SignalSystem.Atc6 ||
                                     activeSignalSystem == E233SignalSystem.Datc;
                if (_powerOff) ShowAtcParts.Value = false;
            }

            Speed.Value = _delayedSpeedProvider.Speed;
            if (_speedTickTracker.TrackAndSync() && ShowAtcParts)
            {
                TurnOff.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcTurnOff);
                if (!_powerOff) SpeedLimit.Value = _panelDataProvider.GetRawValue(LogicalInputIds.AtcSpeedLimit);
                Shunt.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcShunt);
                AbsoluteStop.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcAbsoluteStop);
                PatternApproach.Value = _panelDataProvider.IsActive(LogicalInputIds.AtcPatternApproach);
            }
        }

        protected override void OnReset()
        {
            _normalTickTracker.Reset();
            _speedTickTracker.Reset();
        }
    }
}