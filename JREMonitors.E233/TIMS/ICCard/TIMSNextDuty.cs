using System;

namespace JREMonitors.E233.TIMS.ICCard
{
    public class TIMSNextDuty
    {
        public TIMSNextDuty(string trainNumber, TimeSpan? arrivalTime, TimeSpan? departureTime)
        {
            TrainNumber = trainNumber;
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
        }

        public string TrainNumber { get; }
        public TimeSpan? ArrivalTime { get; }
        public TimeSpan? DepartureTime { get; }
    }
}