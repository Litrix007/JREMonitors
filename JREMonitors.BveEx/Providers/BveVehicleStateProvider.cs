using System;
using System.Reflection;
using BveEx.Extensions.Native;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Registries;
using JREMonitors.Core.Providers;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BveVehicleStateProvider : IVehicleStateProvider, ITickUpdatable, IJumpStationListener
    {
        private static readonly MethodInfo MethodO = typeof(bd).GetMethod("o",
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);

        private readonly Func<double> _getMotorAirBrakeForce;

        private readonly INative _native;
        private readonly BvePanelDataProvider _panelDataProvider;
        private readonly Scenario _scenario;
        private bool _isBack;
        private bool _jumping;

        public BveVehicleStateProvider(INative native, BvePanelDataProvider panelDataProvider, Scenario scenario)
        {
            _native = native;
            _panelDataProvider = panelDataProvider;
            _scenario = scenario;
            var motorCarSrc = scenario.Vehicle.Dynamics.MotorCar.Src;
            _getMotorAirBrakeForce = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), motorCarSrc, MethodO);
        }

        public void OnJumpStation()
        {
            _jumping = true;
        }

        public void Update(TimeSpan elapsed)
        {
            if (_jumping)
            {
                _jumping = false;
                return;
            }

            var vehicleState = _native.VehicleState;
            // 关闭线路时有可能为null
            if (vehicleState == null) return;
            Location = vehicleState.Location;
            Speed = vehicleState.Speed;
            FirstCarBcPressure = vehicleState.BcPressure;
            FirstCarMrPressure = vehicleState.MrPressure;
            ErPressure = vehicleState.ErPressure;
            BpPressure = vehicleState.BpPressure;
            SapPressure = vehicleState.SapPressure;
            Current = vehicleState.Current;
            var brakeSystem = _scenario.Vehicle.Instruments.BrakeSystem;

            IsCompressorWorking = brakeSystem.Compressor.IsWorking;
            var atsPlugin = _scenario.Vehicle.Instruments.AtsPlugin;
            PowerNotch = atsPlugin.AtsHandles.PowerNotch;
            if (_panelDataProvider.HasInput(BuiltinInputIds.BrakeNotch))
                BrakeNotch = _panelDataProvider.GetRawValue(BuiltinInputIds.BrakeNotch);
            else
                BrakeNotch = atsPlugin.AtsHandles.BrakeNotch;

            AreAllDoorClosed = atsPlugin.Doors.AreAllClosed;
            var reverserPosition = atsPlugin.AtsHandles.ReverserPosition;
            if (reverserPosition != ReverserPosition.N) _isBack = reverserPosition == ReverserPosition.B;
            var motorCarInfo = _scenario.Vehicle.Dynamics.MotorCar;
            var rawMotorForce = (float)(motorCarInfo.MotorState.CarForce / 1000);
            MotorForceFeedback = _isBack ? -rawMotorForce : rawMotorForce;
            MotorCarBcPressure = (float)(brakeSystem.MotorCarBrake.BcValve.Pressure.Value / 1000);
            TrailerCarBcPressure = (float)(brakeSystem.TrailerCarBrake.BcValve.Pressure.Value / 1000);
            var totalForce = _getMotorAirBrakeForce();
            var motorCarCount = _scenario.Vehicle.Dynamics.MotorCar.Count;
            MotorAirBrakeForce = (float)(totalForce / motorCarCount / 1000.0);
        }

        public double Location { get; private set; }
        public float Speed { get; private set; }
        public float MotorCarBcPressure { get; private set; }
        public float TrailerCarBcPressure { get; private set; }
        public float FirstCarBcPressure { get; private set; }
        public float FirstCarMrPressure { get; private set; }
        public float ErPressure { get; private set; }
        public float BpPressure { get; private set; }
        public float SapPressure { get; private set; }
        public float Current { get; private set; }
        public bool IsCompressorWorking { get; private set; }
        public int PowerNotch { get; private set; }
        public int BrakeNotch { get; private set; }
        public int TascBrakeNotch => _panelDataProvider.GetRawValue(DirectInputIds.TascBrakeNotch);
        public bool AreAllDoorClosed { get; private set; }
        public float MotorForceFeedback { get; private set; }
        public float MotorAirBrakeForce { get; private set; }
    }
}