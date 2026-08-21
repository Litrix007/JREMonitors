namespace JREMonitors.E233.TIMS.ICCard
{
    public class TIMSMileageCorrectionPoint
    {
        public TIMSMileageCorrectionPoint(int location, int remappedMileage, TIMSMileageDirection mileageDirection)
        {
            Location = location;
            RemappedMileage = remappedMileage;
            MileageDirection = mileageDirection;
        }

        public int Location { get; }
        public int RemappedMileage { get; }
        public TIMSMileageDirection MileageDirection { get; }
    }
}