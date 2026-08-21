using JREMonitors.JRE.Services.Car;

namespace JREMonitors.SandBox.Services
{
    public class MockPassengerStateService : PassengerStateService
    {
        protected override int CarCapacity => 40;
    }
}