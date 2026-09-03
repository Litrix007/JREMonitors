using System;
using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Providers;
using JREMonitors.Core.State;
using BveDoorState = BveTypes.ClassWrappers.DoorState;
using DoorState = JREMonitors.JRE.Services.Car.DoorState;

namespace JREMonitors.BveEx.Services.Car
{
    public class CarDoorController
    {
        private readonly CarDoor _bveLeftCarDoor;
        private readonly CarDoor _bveRightCarDoor;
        private readonly int _carIndex;
        private readonly int _doorCount;
        private readonly SingleDoorState[] _leftDoors;
        private readonly BvePanelDataProvider _panelDataProvider;
        private readonly Random _random;
        private readonly SingleDoorState[] _rightDoors;
        private BveDoorState _prevLeftBveState;
        private BveDoorState _prevRightBveState;
        private bool _latchLeftOpen;
        private bool _latchRightOpen;

        public CarDoorController(DataHub dataHub, Random random, int carIndex, int doorCount, CarDoor bveLeftCarDoor,
            CarDoor bveRightCarDoor)
        {
            _random = random;
            _carIndex = carIndex;
            _doorCount = doorCount;
            _bveLeftCarDoor = bveLeftCarDoor;
            _bveRightCarDoor = bveRightCarDoor;
            _panelDataProvider = dataHub.Get<BvePanelDataProvider>();
            _leftDoors = new SingleDoorState[doorCount];
            _rightDoors = new SingleDoorState[doorCount];

            for (var i = 0; i < doorCount; i++)
            {
                _leftDoors[i] = new SingleDoorState();
                _rightDoors[i] = new SingleDoorState();
            }

            _prevLeftBveState = ToSeededPrevState(bveLeftCarDoor);
            _prevRightBveState = ToSeededPrevState(bveRightCarDoor);

            if (bveLeftCarDoor != null)
            {
                var initialState = bveLeftCarDoor.IsOpen ? DoorState.Opened : DoorState.Closed;
                for (var i = 0; i < doorCount; i++) _leftDoors[i].CurrentState = initialState;
            }

            if (bveRightCarDoor != null)
            {
                var initialState = bveRightCarDoor.IsOpen ? DoorState.Opened : DoorState.Closed;
                for (var i = 0; i < doorCount; i++) _rightDoors[i].CurrentState = initialState;
            }
        }

        public void Update(
            TimeSpan elapsed,
            DoorState[] outLeftStates,
            DoorState[] outRightStates,
            bool forceInstant
        )
        {
            var panelKey = CarDoorIds.At(_carIndex);
            if (_panelDataProvider.HasInput(panelKey))
            {
                var isPanelActive = _panelDataProvider.IsActive(panelKey);
                var isLeftOpening = _bveLeftCarDoor != null &&
                                    (_bveLeftCarDoor.IsOpen || _bveLeftCarDoor.State == BveDoorState.Open);
                var isRightOpening = _bveRightCarDoor != null &&
                                     (_bveRightCarDoor.IsOpen || _bveRightCarDoor.State == BveDoorState.Open);
                if (isPanelActive)
                {
                    if (isLeftOpening && isRightOpening)
                    {
                        _latchLeftOpen = true;
                        _latchRightOpen = true;
                    }
                    else if (isLeftOpening)
                    {
                        _latchLeftOpen = true;
                        _latchRightOpen = false;
                    }
                    else if (isRightOpening)
                    {
                        _latchRightOpen = true;
                        _latchLeftOpen = false;
                    }
                }
                else
                {
                    _latchLeftOpen = false;
                    _latchRightOpen = false;
                }

                if (!isLeftOpening && !isRightOpening && isPanelActive)
                {
                    if (_latchRightOpen) isRightOpening = true;
                    if (_latchLeftOpen) isLeftOpening = true;
                    if (!_latchLeftOpen && !_latchRightOpen) isLeftOpening = true;
                }

                var leftTargetState = isLeftOpening && isPanelActive ? DoorState.Opened : DoorState.Closed;
                var rightTargetState = isRightOpening && isPanelActive ? DoorState.Opened : DoorState.Closed;

                for (var i = 0; i < _doorCount; i++)
                {
                    _leftDoors[i].CurrentState = leftTargetState;
                    _leftDoors[i].OpenDelayRemaining = 0;
                    _leftDoors[i].TransitioningOpen = false;
                    _leftDoors[i].TransitioningClose = false;

                    _rightDoors[i].CurrentState = rightTargetState;
                    _rightDoors[i].OpenDelayRemaining = 0;
                    _rightDoors[i].TransitioningOpen = false;
                    _rightDoors[i].TransitioningClose = false;
                    outLeftStates[i] = leftTargetState;
                    outRightStates[i] = rightTargetState;
                }

                if (_bveLeftCarDoor != null) _prevLeftBveState = _bveLeftCarDoor.State;
                if (_bveRightCarDoor != null) _prevRightBveState = _bveRightCarDoor.State;

                return;
            }

            SimulateSide(elapsed, _bveLeftCarDoor, ref _prevLeftBveState, _leftDoors, outLeftStates, forceInstant);
            SimulateSide(elapsed, _bveRightCarDoor, ref _prevRightBveState, _rightDoors, outRightStates, forceInstant);
        }

        private static BveDoorState ToSeededPrevState(CarDoor door)
        {
            if (door == null) return BveDoorState.Close;
            if (door.IsOpen && door.State == BveDoorState.Close && door.TimeLeftToCompleteClosing > 0)
                return BveDoorState.Open;
            return door.State;
        }

        private void SimulateSide(
            TimeSpan elapsed,
            CarDoor bveCarDoor,
            ref BveDoorState prevBveState,
            SingleDoorState[] doors,
            DoorState[] outStates,
            bool forceInstant
        )
        {
            if (bveCarDoor == null)
            {
                for (var i = 0; i < _doorCount; i++) outStates[i] = DoorState.Closed;
                return;
            }

            if (forceInstant)
            {
                var targetState = bveCarDoor.IsOpen ? DoorState.Opened : DoorState.Closed;
                for (var i = 0; i < _doorCount; i++)
                {
                    doors[i].CurrentState = targetState;
                    doors[i].OpenDelayRemaining = 0;
                    doors[i].CloseThreshold = 0;
                    doors[i].TransitioningOpen = false;
                    doors[i].TransitioningClose = false;
                    outStates[i] = targetState;
                }

                prevBveState = bveCarDoor.State;
                return;
            }

            var currentState = bveCarDoor.State;
            var elapsedMs = (float)elapsed.TotalMilliseconds;

            if (currentState != prevBveState)
            {
                if (currentState == BveDoorState.Open)
                {
                    for (var i = 0; i < _doorCount; i++)
                    {
                        doors[i].TransitioningClose = false;
                        doors[i].TransitioningOpen = true;
                        doors[i].OpenDelayRemaining = (float)(_random.NextDouble() * 150);
                    }
                }
                else if (currentState == BveDoorState.Close)
                {
                    var totalCloseTime = (float)bveCarDoor.TimeLeftToCompleteClosing;
                    if (totalCloseTime <= 0) totalCloseTime = bveCarDoor.CloseTime;

                    var baseCloseTime = (float)bveCarDoor.CloseTime;
                    var stuckTime = Math.Max(0, totalCloseTime - baseCloseTime);

                    if (stuckTime > 0)
                    {
                        var stuckCount = _doorCount >= 2 ? _random.Next(1, 3) : 1;
                        var stuckIndices = new HashSet<int>();
                        var stuckList = new List<int>();

                        while (stuckIndices.Count < stuckCount)
                        {
                            var idx = _random.Next(0, _doorCount);
                            if (stuckIndices.Add(idx)) stuckList.Add(idx);
                        }

                        var slowestStuckIndex = stuckList[_random.Next(0, stuckList.Count)];
                        for (var i = 0; i < _doorCount; i++)
                        {
                            doors[i].TransitioningOpen = false;
                            doors[i].TransitioningClose = true;

                            if (stuckIndices.Contains(i))
                            {
                                if (i == slowestStuckIndex)
                                    doors[i].CloseThreshold = 0f;
                                else
                                    doors[i].CloseThreshold = (float)(50.0 + _random.NextDouble() * 100.0);
                            }
                            else
                            {
                                var closeRatio = (float)(0.95 + _random.NextDouble() * 0.04);
                                doors[i].CloseThreshold = stuckTime + baseCloseTime * (1.0f - closeRatio);
                            }
                        }
                    }
                    else
                    {
                        var slowestIndex = _random.Next(0, _doorCount);
                        for (var i = 0; i < _doorCount; i++)
                        {
                            doors[i].TransitioningOpen = false;
                            doors[i].TransitioningClose = true;

                            if (i == slowestIndex)
                            {
                                doors[i].CloseThreshold = 0f;
                            }
                            else
                            {
                                var closeRatio = (float)(0.95 + _random.NextDouble() * 0.04);
                                doors[i].CloseThreshold = totalCloseTime * (1.0f - closeRatio);
                            }
                        }
                    }
                }

                prevBveState = currentState;
            }

            var bveTimeLeft = (float)bveCarDoor.TimeLeftToCompleteClosing;
            var isBveClosed = !bveCarDoor.IsOpen || bveTimeLeft <= 0;

            for (var i = 0; i < _doorCount; i++)
            {
                if (doors[i].TransitioningOpen)
                {
                    doors[i].OpenDelayRemaining -= elapsedMs;
                    if (doors[i].OpenDelayRemaining <= 0)
                    {
                        doors[i].CurrentState = DoorState.Opened;
                        doors[i].TransitioningOpen = false;
                        doors[i].OpenDelayRemaining = 0;
                    }
                }
                else if (doors[i].TransitioningClose)
                {
                    if (isBveClosed || bveTimeLeft <= doors[i].CloseThreshold)
                    {
                        doors[i].CurrentState = DoorState.Closed;
                        doors[i].TransitioningClose = false;
                        doors[i].CloseThreshold = 0;
                    }
                }
                else
                {
                    doors[i].CurrentState = bveCarDoor.State == BveDoorState.Open ? DoorState.Opened : DoorState.Closed;
                }

                outStates[i] = doors[i].CurrentState;
            }
        }

        private class SingleDoorState
        {
            public DoorState CurrentState { get; set; } = DoorState.Closed;
            public float OpenDelayRemaining { get; set; }
            public float CloseThreshold { get; set; }
            public bool TransitioningOpen { get; set; }
            public bool TransitioningClose { get; set; }
        }
    }
}