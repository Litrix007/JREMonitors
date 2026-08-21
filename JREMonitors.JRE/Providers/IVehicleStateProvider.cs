namespace JREMonitors.JRE.Providers
{
    public interface IVehicleStateProvider
    {
        double Location { get; }
        float Speed { get; }
        float MotorCarBcPressure { get; }
        float TrailerCarBcPressure { get; }
        float FirstCarBcPressure { get; }
        float FirstCarMrPressure { get; }
        float ErPressure { get; }
        float BpPressure { get; }
        float SapPressure { get; }
        bool IsCompressorWorking { get; }
        float Current { get; }
        int PowerNotch { get; }
        int BrakeNotch { get; }
        int TascBrakeNotch { get; }
        bool AreAllDoorClosed { get; }
        float MotorForceFeedback { get; }
        float MotorAirBrakeForce { get; }
    }
}