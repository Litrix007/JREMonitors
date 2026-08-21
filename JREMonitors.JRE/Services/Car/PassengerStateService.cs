using System;
using System.Collections.Generic;
using JREMonitors.Core.Providers;

namespace JREMonitors.JRE.Services.Car
{
    public interface ICarPassengerState
    {
        int PassengerCount { get; }
        int Capacity { get; }
        float LoadFactor { get; }
    }

    public enum PassengerProcessState
    {
        Ready,
        Alighting,
        Boarding,
        Completed
    }

    public abstract class PassengerStateService : ITickUpdatable
    {
        private const float MinCarWeightFactor = 0.65f;
        private const float MaxCarWeightFactor = 1.35f;
        private double _boardStartCount = -1;
        private double _boardTargetCount = -1;
        private string _currentStationId = string.Empty;
        private float[] _currentWeights;
        private float[] _sourceWeights;
        private CarPassengerState[] _states;
        private float[] _targetWeights;
        private IReadOnlyList<int> _unitCarCounts;

        protected bool HasInitialized => _states != null;
        public int CarCount => _states?.Length ?? 0;

        protected abstract int CarCapacity { get; }

        public virtual void Update(TimeSpan elapsed)
        {
        }

        public void Initialize(IReadOnlyList<int> unitCarCounts)
        {
            if (unitCarCounts == null || unitCarCounts.Count == 0) return;

            var oldUnits = ParseSubFormations(_unitCarCounts);
            var newUnits = ParseSubFormations(unitCarCounts);

            var oldCarStates = _states;

            var totalCars = 0;
            for (var i = 0; i < unitCarCounts.Count; i++) totalCars += unitCarCounts[i];

            _unitCarCounts = unitCarCounts;
            _states = new CarPassengerState[totalCars];
            _currentWeights = new float[totalCars];
            _targetWeights = new float[totalCars];
            _sourceWeights = new float[totalCars];

            var capacity = CarCapacity;
            for (var i = 0; i < totalCars; i++) _states[i] = new CarPassengerState(capacity);

            // 编组继承
            if (oldCarStates != null && oldUnits.Count > 0 && newUnits.Count > 0)
            {
                var oldUnitUsed = new bool[oldUnits.Count];
                for (var n = 0; n < newUnits.Count; n++)
                {
                    var newUnit = newUnits[n];
                    for (var o = 0; o < oldUnits.Count; o++)
                    {
                        if (oldUnitUsed[o]) continue;
                        var oldUnit = oldUnits[o];
                        if (oldUnit.CarCount != newUnit.CarCount) continue;
                        for (var k = 0; k < newUnit.CarCount; k++)
                        {
                            var srcIdx = oldUnit.StartIndex + k;
                            var dstIdx = newUnit.StartIndex + k;
                            _states[dstIdx].UpdateCount(oldCarStates[srcIdx].PassengerCount);
                        }

                        oldUnitUsed[o] = true;
                        break;
                    }
                }
            }

            ResetStationFlow();
            RebuildStationWeights(_currentStationId, _targetWeights);
            Array.Copy(_targetWeights, _currentWeights, totalCars);
        }

        public ICarPassengerState GetCarPassengerState(int carIndex)
        {
            if (_states == null || carIndex < 0 || carIndex >= _states.Length)
                return null;
            return _states[carIndex];
        }

        protected void UpdatePassengerData(
            double averageCountPerCar,
            string stationId,
            PassengerProcessState processState,
            bool isJumping = false
        )
        {
            if (_states == null || _states.Length == 0) return;
            var totalPassengers =
                (int)Math.Round(averageCountPerCar * _states.Length, MidpointRounding.AwayFromZero);
            var stationKey = stationId.ToLower();
            // 跳站
            if (isJumping)
            {
                _currentStationId = stationKey;
                RebuildStationWeights(stationKey, _targetWeights);
                Array.Copy(_targetWeights, _currentWeights, _states.Length);
                ResetStationFlow();
                DistributePassengers(totalPassengers, _currentWeights);
                return;
            }

            // 匹配到新车站
            if (_currentStationId != stationKey)
            {
                _currentStationId = stationKey;
                RebuildStationWeights(stationKey, _targetWeights);
                ResetStationFlow();
            }

            if (processState == PassengerProcessState.Boarding)
            {
                if (_boardStartCount < 0)
                {
                    _boardStartCount = totalPassengers;
                    _boardTargetCount = totalPassengers;
                    Array.Copy(_currentWeights, _sourceWeights, _states.Length);
                }

                if (totalPassengers > _boardTargetCount) _boardTargetCount = totalPassengers;
                var p = 0f;
                var boardRange = _boardTargetCount - _boardStartCount;
                if (boardRange > 0)
                {
                    p = (float)((totalPassengers - _boardStartCount) / boardRange);
                    p = Math.Max(0f, Math.Min(1f, p));
                }

                for (var i = 0; i < _states.Length; i++)
                    _currentWeights[i] = (1f - p) * _sourceWeights[i] + p * _targetWeights[i];
            }
            else if (processState == PassengerProcessState.Completed)
            {
                Array.Copy(_targetWeights, _currentWeights, _states.Length);
            }

            DistributePassengers(totalPassengers, _currentWeights);
        }

        private void ResetStationFlow()
        {
            _boardStartCount = -1;
            _boardTargetCount = -1;
        }

        private void RebuildStationWeights(string stationKey, float[] outputWeights)
        {
            if (_states == null || _unitCarCounts == null) return;

            var globalCarIndex = 0;
            var totalGlobalWeight = 0f;

            var minFactor = MinCarWeightFactor;
            var range = Math.Max(0.01f, MaxCarWeightFactor - minFactor);

            for (var unitIdx = 0; unitIdx < _unitCarCounts.Count; unitIdx++)
            {
                var unitCarCount = _unitCarCounts[unitIdx];
                var unitSeed = $"{stationKey}UnitLen{unitCarCount}_{unitIdx}".GetHashCode();
                var rng = new Random(unitSeed);

                for (var i = 0; i < unitCarCount; i++)
                {
                    var weight = (float)(minFactor + rng.NextDouble() * range);
                    outputWeights[globalCarIndex] = weight;
                    totalGlobalWeight += weight;
                    globalCarIndex++;
                }
            }

            if (totalGlobalWeight > 0)
                for (var i = 0; i < outputWeights.Length; i++)
                    outputWeights[i] /= totalGlobalWeight;
        }

        private void DistributePassengers(int totalPassengers, float[] weights)
        {
            var carCount = _states.Length;
            var allocatedSum = 0;

            Span<int> baseAlloc = stackalloc int[carCount];
            Span<float> remainders = stackalloc float[carCount];
            Span<int> indices = stackalloc int[carCount];

            for (var i = 0; i < carCount; i++)
            {
                var exactQuota = totalPassengers * weights[i];
                var floorVal = (int)Math.Floor(exactQuota);
                baseAlloc[i] = floorVal;
                remainders[i] = exactQuota - floorVal;
                indices[i] = i;
                allocatedSum += floorVal;
            }

            var deficit = totalPassengers - allocatedSum;

            for (var i = 0; i < carCount - 1; i++)
            for (var j = i + 1; j < carCount; j++)
                if (remainders[indices[j]] > remainders[indices[i]])
                    (indices[i], indices[j]) = (indices[j], indices[i]);

            for (var i = 0; i < deficit; i++) baseAlloc[indices[i]]++;
            for (var i = 0; i < carCount; i++) _states[i].UpdateCount(baseAlloc[i]);
        }

        private static List<SubFormationUnit> ParseSubFormations(IReadOnlyList<int> formationCarCounts)
        {
            var list = new List<SubFormationUnit>();
            if (formationCarCounts == null) return list;

            var currentStart = 0;
            for (var i = 0; i < formationCarCounts.Count; i++)
            {
                var count = formationCarCounts[i];
                list.Add(new SubFormationUnit(count, currentStart));
                currentStart += count;
            }

            return list;
        }

        private struct SubFormationUnit
        {
            public readonly int CarCount;
            public readonly int StartIndex;

            public SubFormationUnit(int carCount, int startIndex)
            {
                CarCount = carCount;
                StartIndex = startIndex;
            }
        }

        private class CarPassengerState : ICarPassengerState
        {
            public CarPassengerState(int capacity)
            {
                Capacity = capacity;
            }

            public int PassengerCount { get; private set; }
            public int Capacity { get; }
            public float LoadFactor => Capacity > 0 ? (float)PassengerCount / Capacity : 0f;

            public void UpdateCount(int count)
            {
                PassengerCount = Math.Max(0, count);
            }
        }
    }
}