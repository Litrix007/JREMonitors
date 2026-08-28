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

    public readonly struct CarPassengerConfig
    {
        public int SubFormationId { get; }
        public int? Capacity { get; }
        public bool AllowOverload { get; }

        public CarPassengerConfig(int subFormationId, int? capacity, bool allowOverload)
        {
            SubFormationId = subFormationId;
            Capacity = capacity;
            AllowOverload = allowOverload;
        }
    }

    public abstract class PassengerStateService : ITickUpdatable
    {
        // 高峰车厢相比平均水平的最小/最大增幅
        private const float MinPeakAmplification = 0.20f;
        private const float MaxPeakAmplification = 0.40f;

        // 人流向相邻车厢扩散的高斯标准差范围
        private const float MinSpreadSigma = 0.25f;
        private const float MaxSpreadSigma = 0.40f;

        // 车厢微观随机扰动范围
        private const float NoiseScale = 0.08f;
        private const float NoiseOffset = 0.04f;

        // 单车厢相对平均密度的上下限锁
        private const float MinWeightLimit = 0.75f;
        private const float MaxWeightLimit = 1.40f;

        private double _boardStartCount = -1;
        private double _boardTargetCount = -1;
        private IReadOnlyList<CarPassengerConfig> _carConfigs;
        private string _currentStationId = string.Empty;
        private float[] _currentWeights;
        private float[] _sourceWeights;
        private CarPassengerState[] _states;
        private float[] _targetWeights;

        protected bool HasInitialized => _states != null;
        public int CarCount => _states?.Length ?? 0;

        protected abstract int CarCapacity { get; }

        public virtual void Update(TimeSpan elapsed)
        {
        }

        public void Initialize(IReadOnlyList<CarPassengerConfig> carConfigs)
        {
            if (carConfigs == null || carConfigs.Count == 0) return;

            var oldConfigs = _carConfigs;
            var oldCarStates = _states;

            var totalCars = carConfigs.Count;
            _carConfigs = carConfigs;
            _states = new CarPassengerState[totalCars];
            _currentWeights = new float[totalCars];
            _targetWeights = new float[totalCars];
            _sourceWeights = new float[totalCars];

            var defaultCapacity = CarCapacity;

            for (var i = 0; i < totalCars; i++)
            {
                var cfg = carConfigs[i];
                var cap = cfg.Capacity ?? defaultCapacity;
                _states[i] = new CarPassengerState(cap, cfg.AllowOverload);
            }

            if (oldCarStates != null && oldConfigs != null && oldConfigs.Count > 0)
            {
                var oldUnits = ParseSubFormations(oldConfigs);
                var newUnits = ParseSubFormations(carConfigs);
                var oldUnitUsed = new bool[oldUnits.Count];

                for (var n = 0; n < newUnits.Count; n++)
                {
                    var newUnit = newUnits[n];
                    for (var o = 0; o < oldUnits.Count; o++)
                    {
                        if (oldUnitUsed[o]) continue;
                        var oldUnit = oldUnits[o];
                        if (oldUnit.CarCount == newUnit.CarCount)
                        {
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

            if (isJumping)
            {
                _currentStationId = stationKey;
                RebuildStationWeights(stationKey, _targetWeights);
                Array.Copy(_targetWeights, _currentWeights, _states.Length);
                ResetStationFlow();
                DistributePassengers(totalPassengers, _currentWeights);
                return;
            }

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
            if (_states == null || _states.Length == 0) return;

            var totalCars = _states.Length;
            // 混合 stationKey 和车厢数作为种子，确保不同编组下随机性充分
            var seed = stationKey.GetHashCode() ^ (totalCars * 397);
            var rng = new Random(seed);

            // 1. 模拟车站楼梯/出入口数量，随机生成 1 到 2 个人流高峰点
            var numPeaks = rng.NextDouble() > 0.5 ? 2 : 1;

            var peaks = new float[numPeaks];
            var sigmas = new float[numPeaks];
            var amps = new float[numPeaks];

            for (var p = 0; p < numPeaks; p++)
            {
                peaks[p] = (float)rng.NextDouble();
                // 增大扩散半径（0.30 ~ 0.50），使人流分布更平缓，避免两端骤降
                sigmas[p] = 0.30f + (float)rng.NextDouble() * 0.20f;
                // 降低单峰极值（0.10 ~ 0.20），削弱极端拥挤
                amps[p] = 0.10f + (float)rng.NextDouble() * 0.10f;
            }

            var totalWeight = 0f;

            for (var i = 0; i < totalCars; i++)
            {
                var x = totalCars > 1 ? (float)i / (totalCars - 1) : 0.5f;

                var peakFactor = 0f;
                for (var p = 0; p < numPeaks; p++)
                {
                    var dist = (x - peaks[p]) / sigmas[p];
                    peakFactor += amps[p] * (float)Math.Exp(-0.5f * dist * dist);
                }

                // 2. 基础权重：限制在 [0.85, 1.35] 之间，遏制宏观上的巨大落差
                var baseWeight = 1.0f + peakFactor;
                baseWeight = Math.Max(0.85f, Math.Min(1.35f, baseWeight));
                // 3. 关键修复：在上下限截断（Clamp）之后再加入随机噪声（±4%）
                // 这样即使多节车厢被 Clamp 到 0.85，加上噪声后也会变成 0.81~0.89 不等
                // 从而彻底打破权重相同导致的“乘车人数完全重复”现象
                var noise = (float)(rng.NextDouble() * 0.08 - 0.04);
                var weight = baseWeight + noise;
                // 绝对安全边界兜底
                weight = Math.Max(0.70f, Math.Min(1.50f, weight));
                outputWeights[i] = weight;
                totalWeight += weight;
            }

            // 4. 归一化
            if (totalWeight > 0)
                for (var i = 0; i < totalCars; i++)
                    outputWeights[i] /= totalWeight;
        }

        private void DistributePassengers(int totalPassengers, float[] weights)
        {
            var carCount = _states.Length;
            if (carCount == 0) return;

            var allocated = new int[carCount];
            var active = new bool[carCount];

            for (var i = 0; i < carCount; i++)
            {
                allocated[i] = 0;
                active[i] = true;
            }

            var remainingPassengers = Math.Max(0, totalPassengers);

            while (remainingPassengers > 0)
            {
                var activeWeightSum = 0f;
                var activeCount = 0;
                for (var i = 0; i < carCount; i++)
                    if (active[i])
                    {
                        activeWeightSum += weights[i];
                        activeCount++;
                    }

                if (activeCount == 0)
                {
                    var fallbackShare = remainingPassengers / carCount;
                    var fallbackRem = remainingPassengers % carCount;
                    for (var i = 0; i < carCount; i++) allocated[i] += fallbackShare;
                    for (var i = 0; i < fallbackRem; i++) allocated[i]++;
                    break;
                }

                var newlyCapped = false;
                var shares = new float[carCount];

                for (var i = 0; i < carCount; i++)
                {
                    if (!active[i]) continue;
                    var w = activeWeightSum > 0 ? weights[i] / activeWeightSum : 1.0f / activeCount;
                    var share = remainingPassengers * w;

                    var state = _states[i];
                    if (!state.AllowOverload && allocated[i] + share > state.Capacity)
                    {
                        var add = state.Capacity - allocated[i];
                        if (add > 0)
                        {
                            allocated[i] = state.Capacity;
                            remainingPassengers -= add;
                        }

                        active[i] = false;
                        newlyCapped = true;
                    }
                    else
                    {
                        shares[i] = share;
                    }
                }

                if (newlyCapped) continue;

                var floorShares = new int[carCount];
                var remainders = new float[carCount];
                var activeIndices = new List<int>(activeCount);
                var roundAllocationSum = 0;

                for (var i = 0; i < carCount; i++)
                {
                    if (!active[i]) continue;
                    var fl = (int)Math.Floor(shares[i]);
                    floorShares[i] = fl;
                    remainders[i] = shares[i] - fl;
                    roundAllocationSum += fl;
                    activeIndices.Add(i);
                }

                var leftoverInt = remainingPassengers - roundAllocationSum;

                activeIndices.Sort((a, b) => remainders[b].CompareTo(remainders[a]));

                for (var i = 0; i < leftoverInt && i < activeIndices.Count; i++) floorShares[activeIndices[i]]++;

                for (var i = 0; i < carCount; i++)
                    if (active[i])
                        allocated[i] += floorShares[i];

                remainingPassengers = 0;
            }

            for (var i = 0; i < carCount; i++) _states[i].UpdateCount(allocated[i]);
        }

        private static List<SubFormationUnit> ParseSubFormations(IReadOnlyList<CarPassengerConfig> configs)
        {
            var list = new List<SubFormationUnit>();
            if (configs == null || configs.Count == 0) return list;

            var currentId = configs[0].SubFormationId;
            var currentStart = 0;
            var currentCount = 0;

            for (var i = 0; i < configs.Count; i++)
                if (configs[i].SubFormationId == currentId)
                {
                    currentCount++;
                }
                else
                {
                    list.Add(new SubFormationUnit(currentId, currentCount, currentStart));
                    currentId = configs[i].SubFormationId;
                    currentStart = i;
                    currentCount = 1;
                }

            if (currentCount > 0) list.Add(new SubFormationUnit(currentId, currentCount, currentStart));

            return list;
        }

        private readonly struct SubFormationUnit
        {
            public readonly int SubFormationId;
            public readonly int CarCount;
            public readonly int StartIndex;

            public SubFormationUnit(int subFormationId, int carCount, int startIndex)
            {
                SubFormationId = subFormationId;
                CarCount = carCount;
                StartIndex = startIndex;
            }
        }

        private class CarPassengerState : ICarPassengerState
        {
            public CarPassengerState(int capacity, bool allowOverload)
            {
                Capacity = capacity;
                AllowOverload = allowOverload;
            }

            public bool AllowOverload { get; }

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