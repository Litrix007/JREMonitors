using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Providers;
using JREMonitors.JRE.Services.Car;
using Vortice;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXCarStateGroup : Widget<D01AXCarStateGroupViewModel>
    {
        public static readonly Color4 PoweringColor = "#82E6FC".ToColor4();
        public static readonly Color4 RegenerativeColor = Colors.Yellow;
        private readonly D01AXCarDoorGroup _carDoorGroup;
        private readonly TIMSCarGroup _carGroup;
        private readonly BoundsDrawerWidget _doorTitle;
        private readonly TIMSVehicleSpec _vehicleSpec;

        public D01AXCarStateGroup(RenderContext context, TIMSVehicleSpec vehicleSpec, float left = 30) : base(context)
        {
            ViewModel = new D01AXCarStateGroupViewModel(vehicleSpec);
            _vehicleSpec = vehicleSpec;
            Left = CreatePropertySlot(DirtyType.Visual, left);
            _doorTitle = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("ドア", useVerticalOverhangMetrics: true), contentColor: MonitorColors.White);
            _doorTitle.TargetBounds.Bind(CreateComputed(() =>
                new RectangleF(Left.Value + 10, SelfRelativeDirtyBounds.Y + 5, 0, 0)));
            AddChild(_doorTitle);
            _carGroup = new TIMSCarGroup(context, vehicleSpec, true);
            _carGroup.LeftSpacing.Bind(Left);
            _carGroup.RightSpacing.Bind(CreateComputed(() => 800 - SelfRelativeDirtyBounds.Right));
            _carGroup.AnchorY.Value = 400;
            AddChild(_carGroup);
            _carDoorGroup = new D01AXCarDoorGroup(context, vehicleSpec);
            _carDoorGroup.AnchorX.Bind(CreateComputed(() =>
                MathHelper.Lerp(Left.Value, SelfRelativeDirtyBounds.Right, 0.5f)));
            _carDoorGroup.AnchorY.Value = 375;
            _carDoorGroup.FormationSpec.Bind(ViewModel.FormationSpec);
            AddChild(_carDoorGroup);

            WatchEffect(EffectPhase.State, () =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec == null) return;
                var carCount = formationSpec.CarCount;
                var isReverse = _vehicleSpec.IsCarReverseArrangement;
                var i = 0;
                while (i < carCount)
                {
                    var isMotor = formationSpec[i].CarType == TIMSCarType.MotorCar;
                    var carIdx = isReverse ? carCount - i - 1 : i;
                    _carDoorGroup.AllCarDoors[carIdx].IsOpen.Value = ViewModel.IsDoorOpenList[carIdx];

                    if (isMotor)
                    {
                        var hasNextInSpec = i + 1 < carCount;
                        var isNextMotorInSpec =
                            hasNextInSpec && formationSpec[i + 1].CarType == TIMSCarType.MotorCar;

                        if (isNextMotorInSpec)
                        {
                            var nextCarIdx = isReverse ? carCount - (i + 1) - 1 : i + 1;
                            var combinedCurrent = ViewModel.Currents[i] + ViewModel.Currents[i + 1];
                            var color = combinedCurrent > 0 ? PoweringColor :
                                combinedCurrent == 0 ? (Color4?)null : RegenerativeColor;
                            _carGroup.AllCars[carIdx].BackgroundColor.Value = color;
                            _carGroup.AllCars[nextCarIdx].BackgroundColor.Value = color;
                            _carDoorGroup.AllCarDoors[nextCarIdx].IsOpen.Value = ViewModel.IsDoorOpenList[nextCarIdx];
                            i += 2;
                            continue;
                        }

                        var current = ViewModel.Currents[i];
                        _carGroup.AllCars[carIdx].BackgroundColor.Value = current > 0 ? PoweringColor :
                            current == 0 ? (Color4?)null : RegenerativeColor;
                    }
                    else
                    {
                        _carGroup.AllCars[carIdx].BackgroundColor.Value = null;
                    }

                    i++;
                }

                for (var k = carCount; k < TIMSFormationSpec.MaxCarCount; k++)
                {
                    _carGroup.AllCars[k].BackgroundColor.Value = null;
                    _carDoorGroup.AllCarDoors[k].IsOpen.Value = false;
                }
            });
        }

        public PropertySlot<float> Left { get; }

        public sealed override RectangleF SelfRelativeDirtyBounds => new RawRectF(Left.Value, 367, 770, 447);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            Context.DeviceContext.WithAliasedIfNeeded(() =>
            {
                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
            });
        }
    }

    public class D01AXCarStateGroupViewModel : TIMSFormationViewModel
    {
        private CarStateService _carStateService;
        private IDoorStateService _doorStateService;

        public D01AXCarStateGroupViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            Currents = CreateReactiveArray<float>(TIMSFormationSpec.MaxCarCount);
            IsDoorOpenList = CreateReactiveArray<bool>(TIMSFormationSpec.MaxCarCount);
        }

        public ReactiveArray<float> Currents { get; }
        public ReactiveArray<bool> IsDoorOpenList { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _carStateService = dataHub.Get<CarStateService>();
            _doorStateService = dataHub.Get<IDoorStateService>();
            dataHub.Get<TIMSICCardService<E233SignalSystem>>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (FormationSpec.Value == null) return;

            var carCount = FormationSpec.Value.CarCount;
            var vehicleDirection = VehicleDirection.Value;
            var leftDoorStates = _doorStateService.GetLeftDoorStates();
            var rightDoorStates = _doorStateService.GetRightDoorStates();
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
                if (i < carCount)
                {
                    var carIdx = Spec.GetCarIndex(carCount, i);
                    var dataIdx = TIMSVehicleSpec.GetDataIndex(carCount, vehicleDirection, i);
                    Currents[i] = _carStateService.GetCarStateAt(dataIdx)?.Current ?? 0f;
                    IsDoorOpenList[i] = IsDoorOpenAt(leftDoorStates, carIdx) || IsDoorOpenAt(rightDoorStates, carIdx);
                }
                else
                {
                    Currents[i] = 0f;
                    IsDoorOpenList[i] = false;
                }
        }

        private static bool IsDoorOpenAt(DoorState[][] doorStates, int carIndex)
        {
            if (doorStates == null) return false;
            var doorStatesPerCar = doorStates[carIndex];
            for (var i = 0; i < doorStatesPerCar.Length; i++)
                if (doorStatesPerCar[i] == DoorState.Opened)
                    return true;
            return false;
        }
    }
}