using System.Collections.Generic;
using System.Linq;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.SandBox.Services
{
    public class MockDoorStateService : IDoorStateService
    {
        private int _carCount;

        public void Initialize(int carCount, IReadOnlyList<int> doorCountPerCar)
        {
            _carCount = carCount;
        }

        int IDoorStateService.CarCount => _carCount;

        public DoorState[][] GetLeftDoorStates()
        {
            return Enumerable.Range(0, _carCount)
                .Select(_ => Enumerable.Range(0, 4).Select(__ => DoorState.Closed).ToArray())
                .ToArray();
        }

        public DoorState[][] GetRightDoorStates()
        {
            return Enumerable.Range(0, _carCount)
                .Select(_ => Enumerable.Range(0, 4).Select(__ => DoorState.Opened).ToArray())
                .ToArray();
        }
    }
}