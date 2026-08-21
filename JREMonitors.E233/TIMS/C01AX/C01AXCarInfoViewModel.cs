using System;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AXCarInfoViewModel : TIMSFormationViewModel
    {
        private TickTracker _normalTickTracker;
        private PassengerStateService _passengerStateService;

        public C01AXCarInfoViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            PassengerCounts = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            LoadFactors = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            Temperatures = new ReactiveList<float>[TIMSFormationSpec.MaxCarCount];
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++) Temperatures[i] = CreateReactiveList<float>();

            Humidities = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
        }

        public ReactiveArray<int> PassengerCounts { get; }
        public Signal<int> TotalPassengerCount { get; } = new Signal<int>();
        public ReactiveArray<int> LoadFactors { get; }
        public Signal<int> AverageLoadFactor { get; } = new Signal<int>();
        public ReactiveList<float>[] Temperatures { get; }
        public ReactiveArray<int> Humidities { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _passengerStateService = dataHub.Get<PassengerStateService>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (IsFormationSpecChanged || IsVehicleDirectionChanged) _normalTickTracker.Reset();

            if (!_normalTickTracker.TrackAndSync()) return;

            var formationSpec = FormationSpec.Value;
            if (formationSpec == null || _passengerStateService == null) return;

            var carCount = formationSpec.CarCount;
            var totalCapacity = 0;
            TotalPassengerCount.Value = 0;
            for (var carIdx = 0; carIdx < TIMSFormationSpec.MaxCarCount; carIdx++)
                if (carIdx < carCount)
                {
                    var carState = _passengerStateService.GetCarPassengerState(carIdx);
                    if (carState != null)
                    {
                        totalCapacity += carState.Capacity;
                        TotalPassengerCount.Value += carState.PassengerCount;
                        PassengerCounts[carIdx] = carState.PassengerCount;
                        LoadFactors[carIdx] =
                            (int)Math.Round(carState.LoadFactor * 100f, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        PassengerCounts[carIdx] = 0;
                        LoadFactors[carIdx] = 0;
                    }

                    var envState = TIMSService.GetCarEnvironmentState(carIdx);
                    if (envState != null && envState.InteriorTemperatures != null)
                    {
                        Temperatures[carIdx].Update(envState.InteriorTemperatures);
                        Humidities[carIdx] = (int)Math.Round(envState.Humidity, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        Temperatures[carIdx].Clear();
                        Humidities[carIdx] = 0;
                    }
                }
                else
                {
                    PassengerCounts[carIdx] = 0;
                    LoadFactors[carIdx] = 0;
                    Temperatures[carIdx].Clear();
                    Humidities[carIdx] = 0;
                }

            AverageLoadFactor.Value = totalCapacity > 0
                ? (int)Math.Round((float)TotalPassengerCount.Value / totalCapacity * 100f,
                    MidpointRounding.AwayFromZero)
                : 0;
        }

        protected override void OnReset()
        {
            base.OnReset();
            _normalTickTracker.Reset();
        }
    }
}