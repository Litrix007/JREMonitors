using System;
using System.Collections.Generic;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;

namespace JREMonitors.E233.TIMS
{
    public interface ITIMSCarEnvironmentState
    {
        float[] InteriorTemperatures { get; }
        float Humidity { get; }
    }

    public class TIMSService : ITickUpdatable
    {
        public const float DefaultBaseInteriorTemperature = 24;
        public const float DefaultExternalTemperature = 28;
        public const float DefaultBaseHumidity = 70;
        private readonly BlockingService _blockingService;
        private readonly IDebugger _debugger;
        private readonly ITimeProvider _provider;
        private readonly Random _random = new Random();
        private CarEnvironmentState[] _carEnvironmentStates;
        private TIMSFormationSpec _currentFormationSpec;

        private bool _shouldCompleteSetting;

        public TIMSService(
            DataHub dataHub,
            TIMSVehicleDirection vehicleDirection,
            float? baseInteriorTemperature = null,
            float? externalTemperature = null,
            float? baseHumidity = null
        )
        {
            VehicleDirection = vehicleDirection;
            BaseInteriorTemperature = baseInteriorTemperature ?? DefaultBaseInteriorTemperature;
            ExternalTemperature = externalTemperature ?? DefaultExternalTemperature;
            BaseHumidity = baseHumidity ?? DefaultBaseHumidity;
            _debugger = dataHub.GetOrNull<IDebugger>();
            _provider = dataHub.Get<ITimeProvider>();
            _blockingService = dataHub.Get<BlockingService>();
        }

        public float BaseInteriorTemperature { get; set; }
        public float ExternalTemperature { get; set; }
        public float BaseHumidity { get; set; }

        public int TrainSelectionCount { get; set; }
        public TIMSVehicleDirection VehicleDirection { get; protected set; }
        public TIMSSelectionType? CurrentSelectionType { get; private set; }
        public TimeSpan? TrainSelectionTime { get; private set; }
        public bool PassSetting { get; private set; }
        public bool HasPassSetting { get; private set; }
        public bool IsSettingCompleted { get; private set; }
        public bool HasSettingCompleted { get; private set; }

        public virtual void Update(TimeSpan elapsed)
        {
            if (_shouldCompleteSetting)
            {
                CompleteSetting();
                _shouldCompleteSetting = false;
            }

            _debugger?.AddLine($"select count: {TrainSelectionCount}");
            if (TrainSelectionCount <= 0)
            {
                CurrentSelectionType = null;
                TrainSelectionTime = null;
            }
        }

        public ITIMSCarEnvironmentState GetCarEnvironmentState(int carIndex)
        {
            if (_carEnvironmentStates == null || carIndex < 0 || carIndex >= _carEnvironmentStates.Length)
                return null;
            return _carEnvironmentStates[carIndex];
        }

        public void Initialize(TIMSFormationSpec formationSpec)
        {
            if (formationSpec == null || formationSpec.CarCount == 0) return;

            var oldUnits = ParseSubFormations(_currentFormationSpec?.UnitCarCounts);
            var newUnits = ParseSubFormations(formationSpec.UnitCarCounts);
            var oldCarStates = _carEnvironmentStates;
            var totalCars = formationSpec.CarCount;
            var newCarStates = new CarEnvironmentState[totalCars];
            var oldUnitUsed = new bool[oldUnits.Count];
            for (var n = 0; n < newUnits.Count; n++)
            {
                var newUnit = newUnits[n];
                var matchedOldUnitIndex = -1;

                for (var o = 0; o < oldUnits.Count; o++)
                {
                    if (oldUnitUsed[o]) continue;
                    if (oldUnits[o].CarCount == newUnit.CarCount)
                    {
                        matchedOldUnitIndex = o;
                        oldUnitUsed[o] = true;
                        break;
                    }
                }

                if (matchedOldUnitIndex != -1 && oldCarStates != null)
                {
                    var oldUnit = oldUnits[matchedOldUnitIndex];
                    for (var k = 0; k < newUnit.CarCount; k++)
                    {
                        var srcIdx = oldUnit.StartIndex + k;
                        var dstIdx = newUnit.StartIndex + k;
                        newCarStates[dstIdx] = CloneEnvironmentState(oldCarStates[srcIdx]);
                    }
                }
                else
                {
                    for (var k = 0; k < newUnit.CarCount; k++)
                    {
                        var carIdx = newUnit.StartIndex + k;
                        var carSpec = formationSpec[carIdx];
                        newCarStates[carIdx] = GenerateNewCarEnvironmentState(carSpec);
                    }
                }
            }

            _currentFormationSpec = formationSpec;
            _carEnvironmentStates = newCarStates;
        }


        private CarEnvironmentState GenerateNewCarEnvironmentState(TIMSCarSpec carSpec)
        {
            var isGreenCar = carSpec.CarType == TIMSCarType.GreenCar;
            var tempCount = isGreenCar ? 4 : 1;
            var interiorTemps = new float[tempCount];
            for (var i = 0; i < tempCount; i++)
            {
                var offset = (float)(_random.NextDouble() * 2.0 - 1.0);
                interiorTemps[i] =
                    (float)Math.Round(BaseInteriorTemperature + offset, 1, MidpointRounding.AwayFromZero);
            }

            var humOffset = (float)(_random.NextDouble() * 6.0 - 3.0);
            var humidity = (float)Math.Round(BaseHumidity + humOffset, 1, MidpointRounding.AwayFromZero);

            return new CarEnvironmentState
            {
                InteriorTemperatures = interiorTemps,
                Humidity = humidity
            };
        }

        private static CarEnvironmentState CloneEnvironmentState(CarEnvironmentState source)
        {
            if (source == null) return null;
            var temps = new float[source.InteriorTemperatures.Length];
            Array.Copy(source.InteriorTemperatures, temps, source.InteriorTemperatures.Length);
            return new CarEnvironmentState
            {
                InteriorTemperatures = temps,
                Humidity = source.Humidity
            };
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

        public void ResetPassSetting()
        {
            PassSetting = false;
            HasPassSetting = false;
            IsSettingCompleted = false;
            HasSettingCompleted = false;
        }

        public virtual void Reconfigure(
            TIMSVehicleDirection vehicleDirection,
            float? baseInteriorTemperature,
            float? externalTemperature,
            float? baseHumidity)
        {
            VehicleDirection = vehicleDirection;
            BaseInteriorTemperature = baseInteriorTemperature ?? DefaultBaseInteriorTemperature;
            ExternalTemperature = externalTemperature ?? DefaultExternalTemperature;
            BaseHumidity = baseHumidity ?? DefaultBaseHumidity;
        }

        public void CompleteSetting()
        {
            IsSettingCompleted = true;
            HasSettingCompleted = true;
        }

        public void ToggleTrainSelection()
        {
            if (CurrentSelectionType == TIMSSelectionType.TrainSelection)
            {
                TrainSelectionTime = null;
                CurrentSelectionType = null;
                return;
            }

            TrainSelectionTime = _provider.CurrentTime;
            CurrentSelectionType = TIMSSelectionType.TrainSelection;
        }

        public void Apply()
        {
            if (CurrentSelectionType == TIMSSelectionType.TrainSelection)
            {
                HasPassSetting = true;
                PassSetting = !PassSetting;
                IsSettingCompleted = false;
                _blockingService.RequestBlock(
                    TIMSBlockTypes.SetPassSetting,
                    BlockingLevel.AllScreens,
                    () => { _shouldCompleteSetting = true; },
                    1.0f
                );
            }

            CurrentSelectionType = null;
        }

        protected void ForceInstant()
        {
            CurrentSelectionType = null;
            _shouldCompleteSetting = false;
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

        private class CarEnvironmentState : ITIMSCarEnvironmentState
        {
            public float[] InteriorTemperatures { get; set; }
            public float Humidity { get; set; }
        }
    }

    public enum TIMSSelectionType
    {
        TrainSelection
    }
}