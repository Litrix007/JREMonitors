using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Services.Car;
using Vortice.Direct2D1;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABNotchInformation : Widget<D05ABNotchInformationViewModel>
    {
        private readonly Baker _baker;
        private readonly TIMSVehicleSpec _spec;
        private readonly Computed<float> _totalWidth;
        private readonly float _unitWidth;

        public D05ABNotchInformation(RenderContext context, TIMSVehicleSpec spec, float y) :
            base(context, y: y)
        {
            ViewModel = new D05ABNotchInformationViewModel(spec);
            _spec = spec;
            _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
            RegisterResource(_baker);
            _unitWidth = TIMSCarGroup.GetUnitWidth(_spec);
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("受信ﾉｯﾁ", horizontalAlignment: 1, verticalAlignment: 0.5f),
                contentColor: MonitorColors.TIMSTitleGrey, x: -20, y: 10));
            _totalWidth = CreateComputed(() => GetCarGroupTotalWidth(spec, ViewModel.FormationSpec));
            WatchEffect(() => _baker.Refresh(), ViewModel.FormationSpec, ViewModel.VehicleDirection, _totalWidth,
                ViewModel.Notches, ViewModel.Notches2, ViewModel.TascBrakes);
        }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, _totalWidth, 20);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public static float GetCarGroupTotalWidth(TIMSVehicleSpec vehicleSpec, TIMSFormationSpec formationSpec)
        {
            if (formationSpec == null) return 0;
            var firstCarCount = 0;
            for (var i = 0; i < formationSpec.CarCount; i++)
                if (formationSpec[i].CarType == TIMSCarType.FirstCar ||
                    formationSpec[i].CarType == TIMSCarType.LastCar)
                    firstCarCount++;

            var unitCount = formationSpec.CarCount < 10
                ? formationSpec.CarCount + firstCarCount
                : formationSpec.CarCount;
            var unitWidth = TIMSCarGroup.GetUnitWidth(vehicleSpec);
            return unitCount == 0 ? 0 : unitWidth + (unitCount - 1) * (unitWidth + 1);
        }

        private string GetNotchText(int notch, int tascBrake, out bool isEb)
        {
            var text = TIMSHelper.GetNotchText(notch, tascBrake, out isEb);
            if (_unitWidth >= 54 && text.Length <= 2) text = text.ToFullWidth();
            return text;
        }

        protected override void OnDraw(float totalScale)
        {
            if (Context.DeviceContext.GetMaxWorldScale() > 1)
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
            else
                Draw();
        }

        private void Draw()
        {
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;

            var formationSpec = ViewModel.FormationSpec.Value;
            if (formationSpec == null) return;

            Context.CommonBrush.Color = MonitorColors.TIMSTitleGrey;

            var currentX = 0f;
            var hideEbTicks = _spec.MaxFormationCarCount > 12;
            var reduceEbTicks = _spec.MaxFormationCarCount > 10 && _spec.MaxFormationCarCount <= 12;
            var prevLastSlotSuppressed = false;
            var prevLastSlotReduced = false;
            for (var col = 0; col < formationSpec.CarCount; col++)
            {
                var carIdx = _spec.GetCarIndex(formationSpec.CarCount, col);
                var isDoubleUnit = formationSpec.CarCount < 10 &&
                                   (formationSpec[carIdx].CarType == TIMSCarType.FirstCar ||
                                    formationSpec[carIdx].CarType == TIMSCarType.LastCar);
                var carW = isDoubleUnit ? _unitWidth * 2 + 1 : _unitWidth;
                bool firstSlotIsEb;
                bool lastSlotIsEb;
                bool firstSlotLongText;
                bool lastSlotLongText;
                var leftCenterX = 0f;
                var rightCenterX = 0f;
                var leftText = string.Empty;
                var rightText = string.Empty;
                var centerX = 0f;
                var text = string.Empty;
                if (isDoubleUnit)
                {
                    leftCenterX = (float)Math.Floor(currentX + carW / 4f) + 0.5f;
                    rightCenterX = (float)Math.Floor(currentX + 3f * carW / 4f) + 0.5f;
                    var leftNotch = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.Notches2[col]
                        : ViewModel.Notches[col];
                    var leftTascBrake = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.TascBrakes2[col]
                        : ViewModel.TascBrakes[col];
                    var rightNotch = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.Notches[col]
                        : ViewModel.Notches2[col];
                    var rightTascBrake = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.TascBrakes[col]
                        : ViewModel.TascBrakes2[col];

                    leftText = GetNotchText(leftNotch, leftTascBrake, out var leftIsEb);
                    firstSlotIsEb = !string.IsNullOrEmpty(leftText) && leftIsEb;
                    firstSlotLongText = leftText.Length > 2;
                    rightText = GetNotchText(rightNotch, rightTascBrake, out var rightIsEb);
                    lastSlotIsEb = !string.IsNullOrEmpty(rightText) && rightIsEb;
                    lastSlotLongText = rightText.Length > 2;
                }
                else
                {
                    centerX = (float)Math.Floor(currentX + carW / 2f) + 0.5f;
                    text = GetNotchText(ViewModel.Notches[col], ViewModel.TascBrakes[col], out var isEb);
                    firstSlotIsEb = lastSlotIsEb = !string.IsNullOrEmpty(text) && isEb;
                    firstSlotLongText = lastSlotLongText = text.Length > 2;
                }

                var firstSlotSuppressed = firstSlotIsEb && hideEbTicks;
                var firstSlotReduced = (firstSlotIsEb || firstSlotLongText) && reduceEbTicks;
                var lastSlotSuppressed = lastSlotIsEb && hideEbTicks;
                var lastSlotReduced = (lastSlotIsEb || lastSlotLongText) && reduceEbTicks;
                if (col == 0)
                {
                    Context.DeviceContext.DrawLine(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 11.5f),
                        Context.CommonBrush);
                    if (!firstSlotSuppressed)
                        Context.DeviceContext.DrawLine(new Vector2(0.5f, 10.5f),
                            new Vector2(firstSlotReduced ? 4.5f : 7.5f, 10.5f), Context.CommonBrush);
                }
                else
                {
                    var leftX = currentX - 0.5f;
                    Context.DeviceContext.DrawLine(new Vector2(leftX, 0.5f), new Vector2(leftX, 11.5f),
                        Context.CommonBrush);
                    if (!prevLastSlotSuppressed)
                        Context.DeviceContext.DrawLine(
                            new Vector2(prevLastSlotReduced ? leftX - 4f : leftX - 7f, 10.5f),
                            new Vector2(leftX, 10.5f), Context.CommonBrush);
                    if (!firstSlotSuppressed)
                        Context.DeviceContext.DrawLine(new Vector2(leftX, 10.5f),
                            new Vector2(firstSlotReduced ? leftX + 5f : leftX + 8f, 10.5f), Context.CommonBrush);
                }

                if (isDoubleUnit)
                {
                    var unitCenterX = (float)Math.Floor(currentX + carW / 2f) + 0.5f;
                    Context.DeviceContext.DrawLine(new Vector2(unitCenterX, 0.5f), new Vector2(unitCenterX, 11.5f),
                        Context.CommonBrush);
                    if (!firstSlotSuppressed)
                        Context.DeviceContext.DrawLine(
                            new Vector2(unitCenterX - (firstSlotReduced ? 4f : 7f), 10.5f),
                            new Vector2(unitCenterX, 10.5f), Context.CommonBrush);
                    if (!lastSlotSuppressed)
                        Context.DeviceContext.DrawLine(new Vector2(unitCenterX, 10.5f),
                            new Vector2(unitCenterX + (lastSlotReduced ? 5f : 8f), 10.5f), Context.CommonBrush);
                }

                if (isDoubleUnit)
                {
                    if (!string.IsNullOrEmpty(leftText))
                        Context.DeviceContext.DrawDynamicText(Context.DwFactory, leftText, leftCenterX, 10,
                            Context.TIMS().Format18, Context.CommonBrush, 0.5f, 0.5f);
                    if (!string.IsNullOrEmpty(rightText))
                        Context.DeviceContext.DrawDynamicText(Context.DwFactory, rightText, rightCenterX, 10,
                            Context.TIMS().Format18, Context.CommonBrush, 0.5f, 0.5f);
                }
                else
                {
                    if (!string.IsNullOrEmpty(text))
                        Context.DeviceContext.DrawDynamicText(Context.DwFactory, text, centerX, 10,
                            Context.TIMS().Format18, Context.CommonBrush, 0.5f, 0.5f);
                }

                prevLastSlotSuppressed = lastSlotSuppressed;
                prevLastSlotReduced = lastSlotReduced;
                currentX += carW + 1;
            }

            var rightX = _totalWidth.Value - 0.5f;
            Context.DeviceContext.DrawLine(new Vector2(rightX, 0.5f), new Vector2(rightX, 11.5f), Context.CommonBrush);
            if (!prevLastSlotSuppressed)
                Context.DeviceContext.DrawLine(new Vector2(prevLastSlotReduced ? rightX - 3f : rightX - 6f, 10.5f),
                    new Vector2(rightX + 1, 10.5f), Context.CommonBrush);

            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }
    }

    public class D05ABNotchInformationViewModel : D05ABViewModelBase
    {
        private TickTracker _brakeTickTracker;

        public D05ABNotchInformationViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            Notches = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            Notches2 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            TascBrakes = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            TascBrakes2 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
        }

        public ReactiveArray<int> Notches { get; }
        public ReactiveArray<int> Notches2 { get; }
        public ReactiveArray<int> TascBrakes { get; }
        public ReactiveArray<int> TascBrakes2 { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            var delayService = dataHub.Get<DelayService>();
            _brakeTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Brake));
            TickTrackers.Add(_brakeTickTracker);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            _brakeTickTracker.Sync();
        }

        protected override void OnUpdateCarState(int col, ICarState carState)
        {
            if (!_brakeTickTracker.ShouldTrigger) return;
            Notches[col] = TIMSHelper.GetNotchValue(carState.PowerNotch, carState.BrakeNotch);
            Notches2[col] = TIMSHelper.GetNotchValue(carState.PowerNotch2, carState.BrakeNotch2);
            TascBrakes[col] = carState.TascBrakeNotch;
            TascBrakes2[col] = carState.TascBrakeNotch2;
        }
    }
}