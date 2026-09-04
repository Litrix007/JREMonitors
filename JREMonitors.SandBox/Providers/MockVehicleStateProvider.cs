using JREMonitors.JRE.Providers;

namespace JREMonitors.SandBox.Providers
{
    public class MockVehicleStateProvider : IVehicleStateProvider
    {
        public double Location { get; set; } = 300;
        public float Speed { get; set; } = 98;
        public float MotorCarBcPressure { get; set; } = 50;
        public float TrailerCarBcPressure { get; set; } = 50;
        public float FirstCarBcPressure { get; set; } = 100;
        public float FirstCarMrPressure { get; set; } = 800;
        public float ErPressure { get; set; } = 0;
        public float BpPressure { get; set; } = 0;
        public float SapPressure { get; set; } = 0;
        public bool IsCompressorWorking { get; set; } = false;
        public float Current { get; set; } = 0;
        public int PowerNotch { get; set; } = 0;
        public int BrakeNotch { get; set; } = 9;
        public int TascBrakeNotch => 2;
        public bool AreAllDoorClosed { get; set; } = false;
        public float MotorForceFeedback { get; set; } = 50;
        public float MotorAirBrakeForce { get; set; } = 50;
    }
}