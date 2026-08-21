using System;
using System.Collections.Generic;
using System.Linq;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.State;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.BveEx.Services.Car
{
    public class BvePassengerStateService : PassengerStateService, IJumpStationListener
    {
        private readonly IDebugger _debugger;
        private readonly Scenario _scenario;
        private readonly List<Station> _stations;
        private bool _jumping;
        private int _lastMatchedStationIndex = -1;

        public BvePassengerStateService(DataHub dataHub)
        {
            _scenario = dataHub.Get<Scenario>();
            _debugger = dataHub.GetOrNull<IDebugger>();
            var bveHacker = dataHub.Get<IBveHacker>();
            _stations = bveHacker.MapLoader.Stations
                .ToListSafe()
                .Select(s => s.Value)
                .OrderBy(s => s.MinStopPosition)
                .ToList();
        }

        protected override int CarCapacity
        {
            get
            {
                var cap = _scenario?.Vehicle?.Passenger?.Capacity ?? 140;
                return cap > 0 ? (int)Math.Round(cap, MidpointRounding.AwayFromZero) : 140;
            }
        }

        public void OnJumpStation()
        {
            _jumping = true;
        }

        public override void Update(TimeSpan elapsed)
        {
            if (!HasInitialized) return;
            var passenger = _scenario.Vehicle.Passenger;
            var bveAverageCount = passenger.Count;
            var location = _scenario.VehicleLocation.Location;
            var currentStation = GetCurrentStation(location);
            var stationId = currentStation != null
                ? $"stn_{_lastMatchedStationIndex}_{currentStation.Name}"
                : "default";
            var processState = ConvertBveProcessState(passenger.State);
            var isJumping = _jumping;
            if (_jumping) _jumping = false;

            UpdatePassengerData(bveAverageCount, stationId, processState, isJumping);
            _debugger?.AddLine(
                $"passenger stationId:{stationId}");
            _debugger?.AddLine(
                $"passenger service counts:{string.Join(",", Enumerable.Range(0, CarCount).Select(GetCarPassengerState).Select(s => s.PassengerCount))}");
            _debugger?.AddLine(
                $"passenger service total count:{Enumerable.Range(0, CarCount).Select(GetCarPassengerState).Select(s => s.PassengerCount).Sum()}");
        }

        private static PassengerProcessState ConvertBveProcessState(Passenger.StationProcess bveState)
        {
            switch (bveState)
            {
                case Passenger.StationProcess.Alighting:
                    return PassengerProcessState.Alighting;
                case Passenger.StationProcess.Boarding:
                    return PassengerProcessState.Boarding;
                case Passenger.StationProcess.Completed:
                    return PassengerProcessState.Completed;
                case Passenger.StationProcess.Ready:
                default:
                    return PassengerProcessState.Ready;
            }
        }

        private Station GetCurrentStation(double vehicleLocation)
        {
            if (_stations.Count == 0) return null;
            var index = _lastMatchedStationIndex;
            var count = _stations.Count;

            if (index >= count) index = count - 1;
            if (index < 0) index = 0;

            while (index >= 0 && _stations[index].MinStopPosition > vehicleLocation)
                index--;

            while (index < count - 1 && _stations[index + 1].MinStopPosition <= vehicleLocation)
                index++;

            _lastMatchedStationIndex = Math.Max(0, index);
            return _stations[_lastMatchedStationIndex];
        }
    }
}