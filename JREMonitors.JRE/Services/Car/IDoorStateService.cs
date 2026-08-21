using System.Collections.Generic;

namespace JREMonitors.JRE.Services.Car
{
    public enum DoorState
    {
        Closed,
        Opened
    }

    public interface IDoorStateService
    {
        int CarCount { get; }
        void Initialize(int carCount, IReadOnlyList<int> doorCountPerCar);
        DoorState[][] GetLeftDoorStates();
        DoorState[][] GetRightDoorStates();
    }
}