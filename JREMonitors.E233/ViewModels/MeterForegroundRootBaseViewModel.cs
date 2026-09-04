using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.TIMS;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using DirectInputIds = JREMonitors.E233.Constants.DirectInputIds;

namespace JREMonitors.E233.ViewModels
{
    public class MeterForegroundRootBaseViewModel : ViewModel
    {
        private const double WaitSeconds = 1.5;
        private const double HighlightInterval = 0.5;
        private TickTracker _brakeTickTracker;
        private DelayedSpeedProvider _delayedSpeedProvider;
        private double _lowBcPressureTime;
        private E233MonitorStates _monitorStates;
        private TickTracker _normalTickTracker;
        private IPanelDataProvider _panelDataProvider;
        private IVehicleStateProvider _vehicleStateProvider;

        public Signal<float> DeviceVoltage { get; } = new Signal<float>();
        public Signal<float> CatenaryVoltage { get; } = new Signal<float>();
        public Signal<int> Speed { get; } = new Signal<int>();
        public Signal<float> BcPressure { get; } = new Signal<float>(800);
        public Signal<float> MrPressure { get; } = new Signal<float>(1000);
        public Signal<float> Current { get; } = new Signal<float>();
        public Signal<int> Brake { get; } = new Signal<int>();
        public Signal<bool> Eb { get; } = new Signal<bool>();
        public Signal<bool> HoldSpeed { get; } = new Signal<bool>();
        public Signal<bool> Highlight200Kpa { get; } = new Signal<bool>();
        public Signal<bool> IsSafetyLampsVisibleExternally { get; } = new Signal<bool>();
        public Signal<bool> IsSafetyLampVisible { get; } = new Signal<bool>();
        public Signal<bool> SupportsTasc { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            var delayService = dataHub.Get<DelayService>();
            _brakeTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Brake));
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
            _delayedSpeedProvider = dataHub.Get<DelayedSpeedProvider>();
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            IsSafetyLampsVisibleExternally.Value = _monitorStates.IsSafetyLampsVisibleExternally;
            IsSafetyLampVisible.Value = _monitorStates.IsSafetyLampVisibleOnMeterScreen;
            SupportsTasc.Value = _monitorStates.SupportsTasc;
            if (_normalTickTracker.TrackAndSync())
            {
                DeviceVoltage.Value = _panelDataProvider.GetRawValue(JRE.Constants.DirectInputIds.DeviceVoltage);
                CatenaryVoltage.Value = _panelDataProvider.GetRawValue(DirectInputIds.CatenaryVoltage);
                BcPressure.Value = TIMSHelper.GetImpreciseValue(_vehicleStateProvider.FirstCarBcPressure, 10);
                MrPressure.Value = (float)Math.Round(_vehicleStateProvider.FirstCarMrPressure,
                    MidpointRounding.AwayFromZero);
                Current.Value = _vehicleStateProvider.Current;
            }

            if (_brakeTickTracker.TrackAndSync())
            {
                Eb.Value = _vehicleStateProvider.BrakeNotch > BrakeBackground.MaxBrake;
                Brake.Value = _vehicleStateProvider.BrakeNotch;
                HoldSpeed.Value = _panelDataProvider.IsActive(JRE.Constants.DirectInputIds.HoldSpeed);
            }

            Speed.Value = _delayedSpeedProvider.Speed;
            if (!_vehicleStateProvider.AreAllDoorClosed && _vehicleStateProvider.FirstCarBcPressure < 200)
                _lowBcPressureTime += elapsed.TotalSeconds;
            else
                _lowBcPressureTime = 0;

            if (_lowBcPressureTime >= WaitSeconds)
                Highlight200Kpa.Value =
                    (int)((_lowBcPressureTime - WaitSeconds) / HighlightInterval) % 2 == 0;
            else
                Highlight200Kpa.Value = false;
        }

        protected override void OnReset()
        {
            _normalTickTracker.Reset();
            _brakeTickTracker.Reset();
            _lowBcPressureTime = 0;
        }

        public void ToggleLocalSafetyLampVisibility()
        {
            _monitorStates.ToggleLocalSafetyLampVisibility();
        }
    }
}