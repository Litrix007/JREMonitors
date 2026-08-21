using System;
using System.Collections.Generic;
using System.Linq;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE.Services.Car;
using DoorState = JREMonitors.JRE.Services.Car.DoorState;
using BveDoorState = BveTypes.ClassWrappers.DoorState;

namespace JREMonitors.BveEx.Services.Car
{
    public class BveDoorStateService : IDoorStateService, ITickUpdatable, IJumpStationListener
    {
        private readonly DataHub _dataHub;
        private readonly IDebugger _debugger;
        private readonly Random _random = new Random();
        private readonly Scenario _scenario;
        private CarDoorController[] _cars;
        private int[] _doorCountPerCarCache;
        private bool _firstUpdate = true;
        private bool _jumping;
        private DoorState[][] _leftStatesCache;
        private DoorState[][] _rightStatesCache;
        private bool _shouldForceInstant;

        public BveDoorStateService(DataHub dataHub)
        {
            _dataHub = dataHub;
            _debugger = dataHub.GetOrNull<IDebugger>();
            _scenario = dataHub.Get<Scenario>();
        }

        public int CarCount => _cars?.Length ?? 0;

        public void Initialize(int carCount, IReadOnlyList<int> doorCountPerCar)
        {
            if (_cars != null && _cars.Length == carCount && AreDoorCountsEqual(doorCountPerCar)) return;
            var vehicle = _scenario.Vehicle;
            var leftSideDoors = vehicle.Doors.GetSide(DoorSide.Left);
            var rightSideDoors = vehicle.Doors.GetSide(DoorSide.Right);
            DoorRepairer.CleanUpOldDoors(vehicle);
            // HACK 不能用vehicle.Doors.SetCarLength，会导致车门永远无法关闭
            SetCarDoorLength(leftSideDoors, carCount);
            SetCarDoorLength(rightSideDoors, carCount);
            DoorRepairer.RepairDoorSounds(vehicle);
            _doorCountPerCarCache = doorCountPerCar.ToArray();
            _cars = new CarDoorController[carCount];
            _leftStatesCache = new DoorState[carCount][];
            _rightStatesCache = new DoorState[carCount][];
            for (var i = 0; i < carCount; i++)
            {
                var doorCount = doorCountPerCar[i];
                _leftStatesCache[i] = new DoorState[doorCount];
                _rightStatesCache[i] = new DoorState[doorCount];
                var bveLeftCarDoor = leftSideDoors.CarDoors[i];
                var bveRightCarDoor = rightSideDoors.CarDoors[i];
                _cars[i] = new CarDoorController(_dataHub, _random, i, doorCount, bveLeftCarDoor, bveRightCarDoor);
            }
        }

        public DoorState[][] GetLeftDoorStates()
        {
            return _leftStatesCache;
        }

        public DoorState[][] GetRightDoorStates()
        {
            return _rightStatesCache;
        }

        public void OnJumpStation()
        {
            if (_cars == null) return;
            _jumping = true;
        }

        public void Update(TimeSpan elapsed)
        {
            if (_cars == null) return;

            if (_jumping)
            {
                _jumping = false;
                _shouldForceInstant = true;
                return;
            }

            var forceInstant = false;
            if (_firstUpdate || _shouldForceInstant)
            {
                _firstUpdate = false;
                _shouldForceInstant = false;
                forceInstant = true;
            }

            for (var i = 0; i < _cars.Length; i++)
                _cars[i].Update(elapsed, _leftStatesCache[i], _rightStatesCache[i], forceInstant);

            DebugDoors();
        }

        private static void SetCarDoorLength(SideDoorSet doorSet, int length)
        {
            doorSet.SetCarLength(length);
            doorSet.SetState(doorSet.IsOpen ? BveDoorState.Open : BveDoorState.Close);
        }

        private void DebugDoors()
        {
            if (_debugger == null) return;
            _debugger?.Add("left door final states:");
            DebugDoors(_leftStatesCache);
            _debugger?.Add("right door final states:");
            DebugDoors(_rightStatesCache);
        }

        private void DebugDoors(DoorState[][] states)
        {
            _debugger?.AddLine(string.Join(",",
                states.Select(statesPerCar =>
                    string.Join("", statesPerCar.Select(s => s == DoorState.Opened ? 1 : 0)))));
        }

        private bool AreDoorCountsEqual(IReadOnlyList<int> newCounts)
        {
            if (_doorCountPerCarCache == null || _doorCountPerCarCache.Length != newCounts.Count)
                return false;
            for (var i = 0; i < newCounts.Count; i++)
                if (_doorCountPerCarCache[i] != newCounts[i])
                    return false;

            return true;
        }
    }
}