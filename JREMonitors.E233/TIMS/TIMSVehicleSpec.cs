using System.Collections.Generic;
using System.Linq;
using JREMonitors.JRE.Services.Car;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSVehicleSpec
    {
        public TIMSVehicleSpec(
            string vehicleName,
            IReadOnlyDictionary<string, TIMSFormationSpec> formationSpecs,
            bool isCarReverseArrangement,
            bool isPantoGraphReversed,
            Color3? overrideCarStrokeColor,
            D01AXSpec d01axSpec,
            C01AASpec c01aaSpec,
            bool supportsPartialDoorOperation
        )
        {
            VehicleName = vehicleName;
            FormationSpecs = formationSpecs;
            OverrideCarStrokeColor = overrideCarStrokeColor;
            IsCarReverseArrangement = isCarReverseArrangement;
            IsPantoGraphReversed = isPantoGraphReversed;
            D01AXSpec = d01axSpec;
            C01AASpec = c01aaSpec;
            SupportsPartialDoorOperation = supportsPartialDoorOperation;
            MaxFormationCarCount = FormationSpecs.Values.Select(s => s.CarCount).Max();
            HasGreenCar = FormationSpecs.Values.Any(s => s.HasGreenCar);
        }

        public string VehicleName { get; }
        public IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs { get; }
        public Color3? OverrideCarStrokeColor { get; }
        public bool IsCarReverseArrangement { get; }
        public bool IsPantoGraphReversed { get; }
        public D01AXSpec D01AXSpec { get; }
        public C01AASpec C01AASpec { get; }
        public bool SupportsPartialDoorOperation { get; }
        public int MaxFormationCarCount { get; }
        public bool HasGreenCar { get; }

        public int GetCarIndex(int carCount, int i)
        {
            return IsCarReverseArrangement ? carCount - i - 1 : i;
        }

        public TIMSFormationSpec GetFormationSpec(string formation)
        {
            if (formation == null || !FormationSpecs.TryGetValue(formation, out var formationSpec)) return null;
            return formationSpec;
        }

        public static int GetDataIndex(int carCount, TIMSVehicleDirection vehicleDirection, int i)
        {
            return vehicleDirection == TIMSVehicleDirection.Right ? carCount - 1 - i : i;
        }
    }

    public class TIMSCarSpec
    {
        public TIMSCarSpec(
            int carNumber,
            TIMSCarType carType,
            TIMSCarPantoGraphType pantoGraphType,
            int doorCount,
            bool hasSiv,
            bool hasCompressor,
            bool hasWaterTank
        )
        {
            CarNumber = carNumber;
            CarType = carType;
            PantoGraphType = pantoGraphType;
            DoorCount = doorCount;
            HasSiv = hasSiv;
            HasCompressor = hasCompressor;
            HasWaterTank = hasWaterTank;
        }

        public int CarNumber { get; }
        public TIMSCarType CarType { get; }
        public TIMSCarPantoGraphType PantoGraphType { get; }
        public int DoorCount { get; }
        public bool HasSiv { get; }
        public bool HasCompressor { get; }
        public bool HasWaterTank { get; }
    }

    public class TIMSFormationSpec
    {
        public const int MaxCarCount = 15;
        public const int MaxGreenCarCount = 2;
        public const int GreenCarCapacity = 90;

        public TIMSFormationSpec(bool supportsSuica, IReadOnlyList<TIMSCarSpec> cars)
        {
            SupportsSuica = supportsSuica;
            Cars = cars;
            CarCount = cars.Count;
            var doorCountPerCar = new List<int>(CarCount);
            var unitCarCounts = new List<int>();
            var greenCarIndices = new List<int>();
            var carPassengerConfigs = new List<CarPassengerConfig>(CarCount);

            var currentUnitCount = 0;
            var currentSubFormationId = 0;

            for (var i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                doorCountPerCar.Add(car.DoorCount);
                currentUnitCount++;
                var capacity = car.CarType == TIMSCarType.GreenCar ? GreenCarCapacity : (int?)null;
                var allowOverload = car.CarType != TIMSCarType.GreenCar;
                carPassengerConfigs.Add(new CarPassengerConfig(currentSubFormationId, capacity, allowOverload));
                if (car.CarType == TIMSCarType.GreenCar)
                {
                    HasGreenCar = true;
                    greenCarIndices.Add(i);
                }

                if (car.HasWaterTank) HasWaterTank = true;
                var isEndOfUnit = car.CarType == TIMSCarType.LastCar || i == cars.Count - 1;
                if (isEndOfUnit)
                {
                    unitCarCounts.Add(currentUnitCount);
                    currentUnitCount = 0;
                    currentSubFormationId++;
                }
            }

            DoorCountPerCar = doorCountPerCar;
            UnitCarCounts = unitCarCounts;
            GreenCarIndices = greenCarIndices;
            CarPassengerConfigs = carPassengerConfigs;
        }

        public int CarCount { get; }
        public bool SupportsSuica { get; }
        public IReadOnlyList<TIMSCarSpec> Cars { get; }
        public IReadOnlyList<int> DoorCountPerCar { get; }
        public IReadOnlyList<int> UnitCarCounts { get; }
        public IReadOnlyList<int> GreenCarIndices { get; }
        public IReadOnlyList<CarPassengerConfig> CarPassengerConfigs { get; }
        public bool HasGreenCar { get; }
        public bool HasWaterTank { get; }
        public TIMSCarSpec this[int index] => Cars[index];
    }

    public class D01AXSpec
    {
        public D01AXSpec(
            bool mayShowRouteSetInformation,
            bool alignCarStateToRouteSetInformation,
            bool hideNextDutyBackgroundWhenEmpty,
            Color3? overrideDoorOpenBackgroundColor,
            Color3? overrideDoorOpenTextColor
        )
        {
            MayShowRouteSetInformation = mayShowRouteSetInformation;
            AlignCarStateToRouteSetInformation = alignCarStateToRouteSetInformation;
            HideNextDutyBackgroundWhenEmpty = hideNextDutyBackgroundWhenEmpty;
            OverrideDoorOpenBackgroundColor = overrideDoorOpenBackgroundColor;
            OverrideDoorOpenTextColor = overrideDoorOpenTextColor;
        }

        public bool MayShowRouteSetInformation { get; }
        public bool AlignCarStateToRouteSetInformation { get; }
        public bool HideNextDutyBackgroundWhenEmpty { get; }
        public Color3? OverrideDoorOpenBackgroundColor { get; }
        public Color3? OverrideDoorOpenTextColor { get; }
    }

    public class C01AASpec
    {
        public C01AASpec(bool countPassengers)
        {
            CountPassengers = countPassengers;
        }

        public bool CountPassengers { get; }
    }
}