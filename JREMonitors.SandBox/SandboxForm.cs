using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.E233;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen;
using JREMonitors.E233.TidScreen;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.S00AA;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using JREMonitors.JRE.Services;
using JREMonitors.JRE.Services.Car;
using JREMonitors.SandBox.Providers;
using JREMonitors.SandBox.Services;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;
using Screen = JREMonitors.Core.Monitors.Screen;

namespace JREMonitors.SandBox
{
    public class SandboxForm : Form
    {
        private readonly MonitorContext _context;
        private readonly Dictionary<string, int> _data;
        private readonly DataHub _dataHub;
        private readonly DebugForm _debugForm;
        private readonly DelayedSpeedProvider _delayedSpeedProvider;
        private readonly DelayService _delayService;
        private readonly MockTIMSICCardService _mockTIMSICCardService;
        private readonly MockVehicleStateProvider _mockVehicleStateProvider;
        private readonly Monitor _monitor1;
        private readonly Monitor _monitor2;
        private readonly Monitor _monitor3;
        private readonly E233MonitorStateController _monitorStateController;
        private readonly SandboxMonitorManager _sandboxManager;
        private readonly TickUpdateManager _tickUpdateManager;
        private readonly SystemTimeProvider _timeProvider;
        private readonly MockTIMSService _timsService;
        private bool _start = true;

        public SandboxForm()
        {
            _dataHub = new DataHub();
            _debugForm = DebugForm.Start(parentHandle: Handle);
            _dataHub.Put(_debugForm);
            _data = new Dictionary<string, int>
            {
                { DirectInputIds.TascFixedDistance, 1 },
                { DirectInputIds.VehicleDoorAllClosed, 1 },
                { DirectInputIds.TascFailure, 1 },
                { LogicalInputIds.AtcServiceBrake, 1 },
                { LogicalInputIds.AtcShunt, 1 },
                { LogicalInputIds.AtcPower, 1 },
                { LogicalInputIds.AtcSpeedLimit, 99 },
                { LogicalInputIds.AtcTurnOff, 0 },
                { DirectInputIds.DeviceVoltage, 105 },
                { E233.Constants.DirectInputIds.CatenaryVoltage, 1400 }
            };
            _tickUpdateManager = new TickUpdateManager();
            var blockingService = new BlockingService();
            _tickUpdateManager.Register(blockingService);
            _dataHub.Put(blockingService);
            _dataHub.Put(new MockPanelDataProvider(_data));
            _mockVehicleStateProvider = new MockVehicleStateProvider();
            _dataHub.Put(_mockVehicleStateProvider);
            _timeProvider = new SystemTimeProvider();
            _tickUpdateManager.Register(_timeProvider);
            _dataHub.Put(_timeProvider);
            _delayService = JREDelayServices.CreateJREDelayService();
            _tickUpdateManager.Register(_delayService);
            _dataHub.Put(_delayService);
            _delayedSpeedProvider = new DelayedSpeedProvider(_mockVehicleStateProvider, _delayService);
            _tickUpdateManager.Register(_delayedSpeedProvider);
            _dataHub.Put(_delayedSpeedProvider);
            var mockSignalProvider = new MockSignalProvider<E233SignalSystem>();
            _dataHub.Put(mockSignalProvider);
            _monitorStateController = new E233MonitorStateController
            {
                MonitorType = E233MonitorType.TIMSMain,
                IsSafetyLampsVisibleExternally = false
            };
            _dataHub.Put(_monitorStateController);
            var carStateService = new CarStateService(_dataHub);
            _tickUpdateManager.Register(carStateService);
            _dataHub.Put(carStateService);
            var mockDoorStateService = new MockDoorStateService();
            _dataHub.Put(mockDoorStateService);
            _timsService = new MockTIMSService(_dataHub, TIMSVehicleDirection.Right);
            _tickUpdateManager.Register(_timsService);
            _dataHub.Put(_timsService);
            var mockPassengerStateService = new MockPassengerStateService();
            _tickUpdateManager.Register(mockPassengerStateService);
            _dataHub.Put(mockPassengerStateService);
            var formationSpecs = TIMSFormationSpecs.FormationSpecs1000;
            _mockTIMSICCardService = new MockTIMSICCardService(_dataHub, formationSpecs);
            _tickUpdateManager.Register(_mockTIMSICCardService,
                _mockTIMSICCardService.BeforeDeps.OfType<ITickUpdatable>());
            _dataHub.Put(_mockTIMSICCardService);
            _context = new MonitorContext(_debugForm);
            var size1 = new Size(1920, 1440);
            var size2 = new Size(800, 600);
            var size3 = new Size(800, 600);
            _monitor1 = new Monitor(MonitorIds.Monitor1, _dataHub, _context,
                context => new Screen[] { new S00AAScreen(context), new MeterScreen1000(context) }, ScreenIds.S00AA,
                () => false);
            _monitor2 = new Monitor(MonitorIds.Monitor2, _dataHub, _context,
                context => E233Screens.CreateE233Screens1000(context, "SandBox"), ScreenIds.S00AB);
            _monitor3 = new Monitor(MonitorIds.Monitor3, _dataHub, _context,
                context => E233Screens.CreateE233Screens1000(context, "SandBox", new[] { new TidScreen1000(context) }),
                ScreenIds.Tid);
            _monitor1.LocalDataHub.Put(new E233MonitorStateController());
            _monitor2.LocalDataHub.Put(new E233MonitorStateController());
            _monitor3.LocalDataHub.Put(new E233MonitorStateController());
            var monitorDict = new Dictionary<string, Monitor>
            {
                [MonitorIds.Monitor1] = _monitor1,
                [MonitorIds.Monitor2] = _monitor2,
                [MonitorIds.Monitor3] = _monitor3
            };
            var mediator = new E233MonitorInterlockMediator(monitorDict);
            _tickUpdateManager.Register(mediator);
            _dataHub.Put(mediator);
            _sandboxManager = new SandboxMonitorManager(_context, _dataHub, Handle,
                new[] { (_monitor1, size1), (_monitor2, size2) });
            Text = "JREMonitors SandBox";
            ClientSize = new Size(400, 300);
            KeyPreview = true;
            KeyDown += OnKeyDown;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            _debugForm.Show();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var timer = new Timer { Interval = 16 };
            timer.Tick += (s, args) => RenderLoop();
            timer.Start();
        }

        private void RenderLoop()
        {
            if (!_start) return;
            _debugForm.ClearLeftAll();
            _tickUpdateManager.Update(_timeProvider.Elapsed);
            _sandboxManager.Render(_timeProvider.Elapsed);
            _debugForm.Commit();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            var statusText = _start ? "运行中" : "已暂停";
            e.Graphics.DrawString(
                $"Space: 暂停/恢复 (当前状态: {statusText})",
                Font,
                Brushes.White,
                10,
                10
            );
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _sandboxManager.SaveSnapshots();
                    break;
                case Keys.Space:
                    _start = !_start;
                    if (!_start) _timeProvider.Reset();
                    Invalidate(false);
                    break;
                case Keys.B:
                    _mockVehicleStateProvider.FirstCarBcPressure -= 10;
                    _mockVehicleStateProvider.MotorCarBcPressure -= 20;
                    _mockVehicleStateProvider.MotorAirBrakeForce -= 10;
                    _mockVehicleStateProvider.TrailerCarBcPressure -= 10;
                    _mockVehicleStateProvider.FirstCarMrPressure -= 20;
                    _mockVehicleStateProvider.Speed -= 10;
                    _mockVehicleStateProvider.BrakeNotch--;
                    _data[LogicalInputIds.AtcSpeedLimit] -= 10;
                    break;
                case Keys.N:
                    _mockVehicleStateProvider.FirstCarBcPressure += 10;
                    _mockVehicleStateProvider.MotorCarBcPressure += 20;
                    _mockVehicleStateProvider.MotorAirBrakeForce += 10;
                    _mockVehicleStateProvider.TrailerCarBcPressure += 10;
                    _mockVehicleStateProvider.FirstCarMrPressure += 20;
                    _mockVehicleStateProvider.Speed += 10;
                    _mockVehicleStateProvider.BrakeNotch++;
                    _data[LogicalInputIds.AtcSpeedLimit] += 10;
                    break;
                case Keys.Left:
                    _mockVehicleStateProvider.Location -= 25;
                    break;
                case Keys.Right:
                    _mockVehicleStateProvider.Location += 25;
                    break;
                case Keys.D1:
                    _monitor2.ChangeScreen(ScreenIds.S00AB);
                    break;
                case Keys.D2:
                    _monitor2.ChangeScreen(ScreenIds.D01AX);
                    break;
                case Keys.D3:
                    _monitor2.ChangeScreen(ScreenIds.D02AA);
                    break;
                case Keys.D4:
                    _monitor2.ChangeScreen(ScreenIds.D05AA);
                    break;
                case Keys.D5:
                    _monitor2.ChangeScreen(ScreenIds.D05AB);
                    break;
                case Keys.D6:
                    _monitor2.ChangeScreen(ScreenIds.C01AA);
                    break;
                case Keys.D7:
                    _monitor2.ChangeScreen(ScreenIds.C01AB);
                    break;
                case Keys.S:
                    _timsService.VehicleDirection = _timsService.VehicleDirection == TIMSVehicleDirection.Left
                        ? TIMSVehicleDirection.Right
                        : TIMSVehicleDirection.Left;
                    break;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _sandboxManager.Dispose();
            _context.Dispose();
            base.OnFormClosed(e);
        }
    }
}