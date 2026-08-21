using JREMonitors.Core.State;
using JREMonitors.E233.TIMS;

namespace JREMonitors.SandBox.Services
{
    public class MockTIMSService : TIMSService
    {
        public MockTIMSService(DataHub dataHub, TIMSVehicleDirection vehicleDirection) : base(dataHub, vehicleDirection)
        {
        }

        public new TIMSVehicleDirection VehicleDirection
        {
            get => base.VehicleDirection;
            set => base.VehicleDirection = value;
        }
    }
}