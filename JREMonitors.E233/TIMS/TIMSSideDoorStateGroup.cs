using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.E233.TIMS
{
    public class TIMSSideDoorStateGroup : Widget<TIMSSideDoorStateGroupViewModel>
    {
        private readonly TIMSideDoorWidget[] _doors = new TIMSideDoorWidget[TIMSFormationSpec.MaxCarCount];
        private readonly Row _row;
        private readonly BoundsDrawerWidget _sideText;
        private readonly TIMSVehicleSpec _spec;

        public TIMSSideDoorStateGroup(RenderContext context, TIMSVehicleSpec spec, bool isTop, float y,
            IValueSignal<float> rectWidth) : base(context,
            y: y)
        {
            ViewModel = new TIMSSideDoorStateGroupViewModel(spec, isTop);
            _spec = spec;
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                var j = i;
                _doors[j] = new TIMSideDoorWidget(context, spec, rectWidth);
                _doors[j].IsVisible.Bind(CreateComputed(() => j < ViewModel.CarCount));
            }

            WatchEffect(EffectPhase.State, () =>
            {
                var carCount = ViewModel.CarCount.Value;
                for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
                    if (i < carCount)
                    {
                        var carIdx = spec.GetCarIndex(carCount, i);
                        _doors[i].DoorStates.Update(ViewModel.Doors[carIdx]);
                    }
            });
            _sideText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(isTop ? spec.IsPantoGraphReversed ? "【332側】" : "【333側】" :
                    spec.IsPantoGraphReversed ? "【333側】" : "【332側】", horizontalAlignment: 1),
                contentColor: MonitorColors.TIMSTitleGrey, y: 1);
            AddChild(_sideText);
            _row = new Row(context, 400, widgets: _doors, widgetSpacing: 1, positionSnapToPixels: true);
            AddChild(_row);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public PropertySlot<float> AnchorX => _row.X;

        protected override void OnArrangeLayout(bool ignoreHidden)
        {
            ArrangeChild(_row, ignoreHidden);
            _sideText.X.Value = _spec.MaxFormationCarCount > 12 ? 92f : _row.X;
            ArrangeChild(_sideText, ignoreHidden);
        }
    }

    public class TIMSSideDoorStateGroupViewModel : TIMSFormationViewModel
    {
        private readonly bool _isTop;
        private IDoorStateService _doorStateService;
        private TIMSService _timsService;

        public TIMSSideDoorStateGroupViewModel(TIMSVehicleSpec spec, bool isTop) : base(spec)
        {
            _isTop = isTop;
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++) Doors[i] = CreateReactiveList<DoorState>();
        }

        public Signal<int> CarCount { get; } = new Signal<int>();

        public ReactiveList<DoorState>[] Doors { get; } = new ReactiveList<DoorState>[TIMSFormationSpec.MaxCarCount];

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _timsService = dataHub.Get<TIMSService>();
            _doorStateService = dataHub.Get<IDoorStateService>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            CarCount.Value = _doorStateService.CarCount;
            var vehicleDirection = _timsService.VehicleDirection;
            var doorStates = _isTop
                ? vehicleDirection == TIMSVehicleDirection.Left
                    ? _doorStateService.GetRightDoorStates()
                    : _doorStateService.GetLeftDoorStates()
                : vehicleDirection == TIMSVehicleDirection.Left
                    ? _doorStateService.GetLeftDoorStates()
                    : _doorStateService.GetRightDoorStates();
            if (doorStates == null) return;

            for (var i = 0; i < doorStates.Length; i++) Doors[i].Update(doorStates[i]);
        }
    }
}