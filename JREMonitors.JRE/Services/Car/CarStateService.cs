using System;
using System.Diagnostics;
using System.Linq;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE.Providers;

namespace JREMonitors.JRE.Services.Car
{
    public interface ICarState
    {
        float MotorCarBcPressure { get; }
        float MotorCarBcPressure2 { get; }
        float TrailerCarBcPressure { get; }
        float TrailerCarBcPressure2 { get; }
        float Current { get; }
        bool IsCompressorWorking { get; }
        int PowerNotch { get; }
        int PowerNotch2 { get; }
        int BrakeNotch { get; }
        int BrakeNotch2 { get; }
        int TascBrakeNotch { get; }
        int TascBrakeNotch2 { get; }
        float MotorForceFeedback { get; }
        float MotorAirBrakeForce { get; }
    }

    public class CarStateService : ITickUpdatable
    {
        private const int HistorySize = 512;
        private const int HistoryMask = HistorySize - 1;
        private const float MotherWaveZeroThreshold = 0.001f;

        // 固有硬件公差
        private const float StaticToleranceRange = 0.04f;
        private const float StaticToleranceOffset = 0.02f;

        // 稳态微调上限与包络比例
        private const float PerturbationEnvelopeRatio = 0.02f;
        private const float PerturbationDriftTau = 0.3f;
        private const float MaxBcPerturbation = 8f;
        private const float MaxForcePerturbation = 1.2f;
        private const float MaxCurrentPerturbation = 10f;

        // 稳态微调间隔周期
        private const float HuntingIntervalMin = 4.0f;
        private const float HuntingIntervalRange = 6.0f;
        private const float HuntingInitialMax = 10.0f;

        // 手柄动作时的随机响应时差范围 
        private const float ActionDelayMin = 0.06f;
        private const float ActionDelayRange = 0.22f;

        private readonly IDebugger _debugger;

        private readonly VehicleStateSample[] _history = new VehicleStateSample[HistorySize];
        private readonly Random _random = new Random();
        private readonly IVehicleStateProvider _vehicleStateProvider;

        private CarState[] _carStates;
        private ChannelSimData[] _channels1;
        private ChannelSimData[] _channels2;
        private float _currentTime;
        private int _historyCount;
        private int _historyWriteIndex;
        private int _prevBrakeNotch;

        private int _prevPowerNotch;
        private int _prevTascNotch;

        public CarStateService(DataHub dataHub)
        {
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            _debugger = dataHub.GetOrNull<IDebugger>();
        }

        public bool HasInitialized => _carStates != null;
        public int CarCount => _carStates?.Length ?? 0;

        public virtual void Update(TimeSpan elapsed)
        {
            if (_carStates == null) return;
            var dt = (float)elapsed.TotalSeconds;
            _currentTime += dt;
            _history[_historyWriteIndex] = new VehicleStateSample
            {
                Time = _currentTime,
                MotorCarBcPressure = _vehicleStateProvider.MotorCarBcPressure,
                TrailerCarBcPressure = _vehicleStateProvider.TrailerCarBcPressure,
                Current = _vehicleStateProvider.Current,
                MotorForceFeedback = _vehicleStateProvider.MotorForceFeedback,
                MotorAirBrakeForce = _vehicleStateProvider.MotorAirBrakeForce,
                PowerNotch = _vehicleStateProvider.PowerNotch,
                BrakeNotch = _vehicleStateProvider.BrakeNotch,
                TascBrakeNotch = _vehicleStateProvider.TascBrakeNotch,
                IsCompressorWorking = _vehicleStateProvider.IsCompressorWorking
            };
            _historyWriteIndex = (_historyWriteIndex + 1) & HistoryMask;
            _historyCount = Math.Min(_historyCount + 1, HistorySize);
            var currPower = _vehicleStateProvider.PowerNotch;
            var currBrake = _vehicleStateProvider.BrakeNotch;
            var currTasc = _vehicleStateProvider.TascBrakeNotch;

            if (currPower != _prevPowerNotch || currBrake != _prevBrakeNotch || currTasc != _prevTascNotch)
            {
                _prevPowerNotch = currPower;
                _prevBrakeNotch = currBrake;
                _prevTascNotch = currTasc;
                ReshuffleActionDelays();
            }

            for (var i = 0; i < _carStates.Length; i++)
            {
                _channels1[i].Update(this, _currentTime, dt, _random);
                _channels2[i].Update(this, _currentTime, dt, _random);
                if (i == 0)
                {
                    _channels1[i].PowerNotch = _vehicleStateProvider.PowerNotch;
                    _channels1[i].BrakeNotch = _vehicleStateProvider.BrakeNotch;
                    _channels1[i].TascBrakeNotch = _vehicleStateProvider.TascBrakeNotch;
                }

                _carStates[i].UpdateFrom(_channels1[i], _channels2[i]);
            }

            Debug();
        }

        public void Initialize(int carCount)
        {
            _carStates = new CarState[carCount];
            _channels1 = new ChannelSimData[carCount];
            _channels2 = new ChannelSimData[carCount];

            for (var i = 0; i < carCount; i++)
            {
                _carStates[i] = new CarState();
                _channels1[i] = new ChannelSimData();
                _channels1[i].Init(_random);
                _channels2[i] = new ChannelSimData();
                _channels2[i].Init(_random);
            }

            _currentTime = 0f;
            _historyCount = 0;
            _historyWriteIndex = 0;
            _prevPowerNotch = _vehicleStateProvider.PowerNotch;
            _prevBrakeNotch = _vehicleStateProvider.BrakeNotch;
            _prevTascNotch = _vehicleStateProvider.TascBrakeNotch;

            ForceInstant();
        }

        protected void ForceInstant()
        {
            if (_carStates == null) return;

            var sample = new VehicleStateSample
            {
                Time = _currentTime,
                MotorCarBcPressure = _vehicleStateProvider.MotorCarBcPressure,
                TrailerCarBcPressure = _vehicleStateProvider.TrailerCarBcPressure,
                Current = _vehicleStateProvider.Current,
                MotorForceFeedback = _vehicleStateProvider.MotorForceFeedback,
                MotorAirBrakeForce = _vehicleStateProvider.MotorAirBrakeForce,
                PowerNotch = _vehicleStateProvider.PowerNotch,
                BrakeNotch = _vehicleStateProvider.BrakeNotch,
                TascBrakeNotch = _vehicleStateProvider.TascBrakeNotch,
                IsCompressorWorking = _vehicleStateProvider.IsCompressorWorking
            };

            for (var i = 0; i < HistorySize; i++)
            {
                _history[i] = sample;
                _history[i].Time = _currentTime - 1f + i * 0.016f;
            }

            _historyCount = HistorySize;

            for (var i = 0; i < _carStates.Length; i++)
            {
                _channels1[i].CurrentDelay = 0f;
                _channels1[i].TargetDelay = 0f;
                _channels2[i].CurrentDelay = 0f;
                _channels2[i].TargetDelay = 0f;
                _channels1[i].Update(this, _currentTime, 10f, _random);
                _channels2[i].Update(this, _currentTime, 10f, _random);
                _carStates[i].UpdateFrom(_channels1[i], _channels2[i]);
            }
        }

        private void ReshuffleActionDelays()
        {
            _channels1[0].TargetDelay = 0f;
            _channels2[0].TargetDelay = 0.02f;

            for (var i = 1; i < _carStates.Length; i++)
            {
                _channels1[i].TargetDelay = ActionDelayMin + (float)_random.NextDouble() * ActionDelayRange;
                _channels2[i].TargetDelay = ActionDelayMin + (float)_random.NextDouble() * ActionDelayRange;
            }
        }

        private VehicleStateSample QuerySampleAt(float queryTime)
        {
            if (_historyCount == 0) return default;

            var lastIdx = (_historyWriteIndex - 1 + HistorySize) & HistoryMask;
            var newest = _history[lastIdx];
            if (queryTime >= newest.Time) return newest;

            var curr = lastIdx;
            var idx1 = -1;
            var idx2 = -1;

            for (var i = 0; i < _historyCount - 1; i++)
            {
                var next = (curr - 1 + HistorySize) & HistoryMask;
                if (queryTime >= _history[next].Time && queryTime <= _history[curr].Time)
                {
                    idx1 = next;
                    idx2 = curr;
                    break;
                }

                curr = next;
            }

            if (idx1 != -1)
            {
                var s1 = _history[idx1];
                var s2 = _history[idx2];
                var span = s2.Time - s1.Time;
                var factor = span > 0.0001f ? (queryTime - s1.Time) / span : 0f;

                return new VehicleStateSample
                {
                    Time = queryTime,
                    MotorCarBcPressure =
                        s1.MotorCarBcPressure + (s2.MotorCarBcPressure - s1.MotorCarBcPressure) * factor,
                    TrailerCarBcPressure = s1.TrailerCarBcPressure +
                                           (s2.TrailerCarBcPressure - s1.TrailerCarBcPressure) * factor,
                    Current = s1.Current + (s2.Current - s1.Current) * factor,
                    MotorForceFeedback =
                        s1.MotorForceFeedback + (s2.MotorForceFeedback - s1.MotorForceFeedback) * factor,
                    MotorAirBrakeForce =
                        s1.MotorAirBrakeForce + (s2.MotorAirBrakeForce - s1.MotorAirBrakeForce) * factor,
                    PowerNotch = s1.PowerNotch,
                    BrakeNotch = s1.BrakeNotch,
                    TascBrakeNotch = s1.TascBrakeNotch,
                    IsCompressorWorking = s1.IsCompressorWorking
                };
            }

            var oldestIdx = (_historyWriteIndex - _historyCount + HistorySize) & HistoryMask;
            return _history[oldestIdx];
        }

        public ICarState GetCarStateAt(int carIndex)
        {
            if (_carStates == null || carIndex < 0 || carIndex >= _carStates.Length)
                return null;
            return _carStates[carIndex];
        }

        [Conditional("DEBUG")]
        private void Debug()
        {
            if (_debugger == null) return;
            _debugger.AddLine(
                $"car state brake1: {string.Join(",", _carStates.Select(d => $"{d.BrakeNotch}"))}");
            _debugger.AddLine(
                $"car state brake2: {string.Join(",", _carStates.Select(d => $"{d.BrakeNotch2}"))}");
            _debugger.AddLine(
                $"car state m-bcp1: {string.Join(",", _carStates.Select(d => $"{d.MotorCarBcPressure:F0}"))}");
            _debugger.AddLine(
                $"car state t-bcp1: {string.Join(",", _carStates.Select(d => $"{d.TrailerCarBcPressure:F0}"))}");
            _debugger.AddLine($"car state current: {string.Join(",", _carStates.Select(d => $"{d.Current:F0}"))}");
            _debugger.AddLine(
                $"car state cp: {string.Join(",", _carStates.Select(d => d.IsCompressorWorking ? "1" : "0"))}");
            _debugger.AddLine(
                $"car state motor force: {string.Join(",", _carStates.Select(d => $"{d.MotorForceFeedback:F0}"))}");
            _debugger.AddLine(
                $"car state air brake force: {string.Join(",", _carStates.Select(d => $"{d.MotorAirBrakeForce:F0}"))}");
        }

        private struct AnalogState
        {
            public float Displayed;
            public float CurrentDelta;
            public float TargetDelta;
            public float Tolerance;
            public float MaxPerturbation;

            public void Update(float motherWave, float dt, bool triggerHunting, Random random)
            {
                if (Math.Abs(motherWave) < MotherWaveZeroThreshold)
                {
                    TargetDelta = 0f;
                    CurrentDelta = 0f;
                    Displayed = 0f;
                    return;
                }

                var maxDelta = Math.Min(MaxPerturbation, Math.Abs(motherWave) * PerturbationEnvelopeRatio);
                if (triggerHunting)
                    TargetDelta = (float)((random.NextDouble() * 2.0 - 1.0) * maxDelta);

                TargetDelta = Math.Max(-maxDelta, Math.Min(maxDelta, TargetDelta));

                var driftAlpha = (float)Math.Exp(-dt / PerturbationDriftTau);
                CurrentDelta = TargetDelta + (CurrentDelta - TargetDelta) * driftAlpha;
                Displayed = motherWave * (1f + Tolerance) + CurrentDelta;
                if (motherWave > 0 && Displayed < 0) Displayed = 0;
                if (motherWave < 0 && Displayed > 0) Displayed = 0;
            }
        }

        private class ChannelSimData
        {
            public AnalogState AirBrakeForce;
            public int BrakeNotch;
            public bool CpWorking;
            public AnalogState Current;
            public float CurrentDelay;

            public float HuntingTimer;

            public AnalogState MotorBc;
            public AnalogState MotorForce;

            public int PowerNotch;
            public float TargetDelay;
            public int TascBrakeNotch;
            public AnalogState TrailerBc;

            public void Init(Random random)
            {
                HuntingTimer = (float)random.NextDouble() * HuntingInitialMax;

                MotorBc.MaxPerturbation = MaxBcPerturbation;
                TrailerBc.MaxPerturbation = MaxBcPerturbation;
                MotorForce.MaxPerturbation = MaxForcePerturbation;
                AirBrakeForce.MaxPerturbation = MaxForcePerturbation;
                Current.MaxPerturbation = MaxCurrentPerturbation;

                MotorBc.Tolerance = (float)(random.NextDouble() * StaticToleranceRange - StaticToleranceOffset);
                TrailerBc.Tolerance = (float)(random.NextDouble() * StaticToleranceRange - StaticToleranceOffset);
                Current.Tolerance = (float)(random.NextDouble() * StaticToleranceRange - StaticToleranceOffset);
                MotorForce.Tolerance = (float)(random.NextDouble() * StaticToleranceRange - StaticToleranceOffset);
                AirBrakeForce.Tolerance = (float)(random.NextDouble() * StaticToleranceRange - StaticToleranceOffset);
            }

            public void Update(CarStateService service, float now, float dt, Random random)
            {
                const float delayTau = 0.08f;
                var delayAlpha = (float)Math.Exp(-dt / delayTau);
                CurrentDelay = TargetDelay + (CurrentDelay - TargetDelay) * delayAlpha;
                var sample = service.QuerySampleAt(now - CurrentDelay);
                PowerNotch = sample.PowerNotch;
                BrakeNotch = sample.BrakeNotch;
                TascBrakeNotch = sample.TascBrakeNotch;
                CpWorking = sample.IsCompressorWorking;
                HuntingTimer -= dt;
                var triggerHunting = false;
                if (HuntingTimer <= 0)
                {
                    triggerHunting = true;
                    HuntingTimer = HuntingIntervalMin + (float)random.NextDouble() * HuntingIntervalRange;
                }

                MotorBc.Update(sample.MotorCarBcPressure, dt, triggerHunting, random);
                TrailerBc.Update(sample.TrailerCarBcPressure, dt, triggerHunting, random);
                Current.Update(sample.Current, dt, triggerHunting, random);
                MotorForce.Update(sample.MotorForceFeedback, dt, triggerHunting, random);
                AirBrakeForce.Update(sample.MotorAirBrakeForce, dt, triggerHunting, random);
            }
        }

        private struct VehicleStateSample
        {
            public float Time;
            public float MotorCarBcPressure;
            public float TrailerCarBcPressure;
            public float Current;
            public float MotorForceFeedback;
            public float MotorAirBrakeForce;
            public int PowerNotch;
            public int BrakeNotch;
            public int TascBrakeNotch;
            public bool IsCompressorWorking;
        }

        private class CarState : ICarState
        {
            public float MotorCarBcPressure { get; private set; }
            public float MotorCarBcPressure2 { get; private set; }
            public float TrailerCarBcPressure { get; private set; }
            public float TrailerCarBcPressure2 { get; private set; }
            public float Current { get; private set; }
            public bool IsCompressorWorking { get; private set; }
            public int PowerNotch { get; private set; }
            public int PowerNotch2 { get; private set; }
            public int BrakeNotch { get; private set; }
            public int BrakeNotch2 { get; private set; }
            public int TascBrakeNotch { get; private set; }
            public int TascBrakeNotch2 { get; private set; }
            public float MotorForceFeedback { get; private set; }
            public float MotorAirBrakeForce { get; private set; }

            public void UpdateFrom(ChannelSimData ch1, ChannelSimData ch2)
            {
                MotorCarBcPressure = ch1.MotorBc.Displayed;
                MotorCarBcPressure2 = ch2.MotorBc.Displayed;
                TrailerCarBcPressure = ch1.TrailerBc.Displayed;
                TrailerCarBcPressure2 = ch2.TrailerBc.Displayed;
                Current = ch1.Current.Displayed;
                IsCompressorWorking = ch1.CpWorking;
                PowerNotch = ch1.PowerNotch;
                PowerNotch2 = ch2.PowerNotch;
                BrakeNotch = ch1.BrakeNotch;
                BrakeNotch2 = ch2.BrakeNotch;
                TascBrakeNotch = ch1.TascBrakeNotch;
                TascBrakeNotch2 = ch2.TascBrakeNotch;
                MotorForceFeedback = ch1.MotorForce.Displayed;
                MotorAirBrakeForce = ch1.AirBrakeForce.Displayed;
            }
        }
    }
}