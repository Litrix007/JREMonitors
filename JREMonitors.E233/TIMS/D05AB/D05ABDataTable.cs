using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.D01AX;
using JREMonitors.E233.ViewModels;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using JREMonitors.JRE.Services.Car;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABDataTable : Widget<D05ABDataTableViewModel>
    {
        public const float TableY = 200;
        private const float TableHeight = 64;

        private readonly Baker _baker;
        private readonly TIMSVehicleSpec _spec;
        private readonly Computed<float> _totalWidth;
        private readonly float _unitWidth;
        protected readonly List<FooterItemConfig> FooterItemConfigs;

        public D05ABDataTable(RenderContext context, TIMSVehicleSpec spec, D05ABDataTableViewModel viewModel = null) :
            base(context, y: TableY)
        {
            ViewModel = viewModel ?? new D05ABDataTableViewModel(spec);
            _spec = spec;
            _unitWidth = TIMSCarGroup.GetUnitWidth(spec);
            _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
            RegisterResource(_baker);
            FirstCarX = CreateRelayPropertySlot<float>();
            _totalWidth = CreateComputed(() =>
                D05ABNotchInformation.GetCarGroupTotalWidth(spec, ViewModel.FormationSpec));
            FooterItemConfigs = new List<FooterItemConfig>
            {
                new FooterItemConfig("ノッチ",
                    CreateComputed(() =>
                    {
                        var onText = TIMSHelper.GetNotchText(ViewModel.Notch, ViewModel.TascBrake, out _);
                        if (onText.Length > 2)
                            onText = onText.Substring(0, 2).ToFullWidth() + onText.Substring(2);
                        else
                            onText = onText.ToFullWidth();

                        onText = onText.PadRight(4);
                        return RichTextParser.Raw(ViewModel.Notch != 0 || ViewModel.TascBrake > 0 ? onText : "ＯＦＦ");
                    }))
            };
            AddFooterItemConfigs();
            AddBoolFooterItemConfig("直予備Ｂ", ViewModel.DirectAirBackupBrake);
            AddBoolFooterItemConfig("耐雪Ｂ", ViewModel.SnowBrake);
            var footerChildren = new List<Widget>();
            foreach (var config in FooterItemConfigs)
            {
                footerChildren.Add(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer(config.Name),
                    contentColor: MonitorColors.TIMSTitleGrey));
                footerChildren.Add(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer(config.Text),
                    contentColor: MonitorColors.White));
            }

            var row = new Row(context, 400, 230, widgetSpacing: 18, widgets: footerChildren,
                rowHorizontalAlignment: 0.5f);
            AddChild(row);
            WatchEffect(() => _baker.Refresh(), ViewModel.FormationSpec, ViewModel.VehicleDirection, _totalWidth,
                ViewModel.BcpList1, ViewModel.BcpList2, ViewModel.MotorForceFeedbackList, ViewModel.AirBrakeForceList,
                ViewModel.NotchList);
        }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, 800, 220);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public PropertySlot<float> FirstCarX { get; }

        protected void AddBoolFooterItemConfig(string name, IValueSignal<RichTextDocument> on)
        {
            FooterItemConfigs.Add(new FooterItemConfig(name, on));
        }

        protected void AddBoolFooterItemConfig(string name, IValueSignal<bool> on)
        {
            FooterItemConfigs.Add(new FooterItemConfig(name,
                CreateComputed(() => RichTextParser.Raw(on.Value ? "ＯＮ\u3000" : "ＯＦＦ"))));
        }

        protected virtual void AddFooterItemConfigs()
        {
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
            var oldTransform = Context.DeviceContext.Transform;
            Context.DeviceContext.Transform = Matrix3x2.CreateTranslation(FirstCarX, 10) * oldTransform;
            DrawBcpTable();
            Context.DeviceContext.Transform =
                Matrix3x2.CreateTranslation(FirstCarX, 10 + TableHeight + 36) * oldTransform;
            DrawRegenerationTable();
            Context.DeviceContext.Transform = oldTransform;
            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }

        private void DrawBarAndText(float barX, int value, int maxValue, Color4 color, float textY = TableHeight + 8)
        {
            var barHeight = MathHelper.Clamp(value, 0, maxValue) / (float)maxValue * TableHeight;
            if (barHeight > 0)
            {
                Context.CommonBrush.Color = color;
                var bottomY = TableHeight + 0.5f;
                var topY = bottomY - barHeight;
                Context.DeviceContext.FillRectangle(new RectangleF(barX, topY, 16, barHeight), Context.CommonBrush);
            }

            Context.CommonBrush.Color = color;
            Context.DeviceContext.DrawDynamicText(
                Context.DwFactory,
                value.ToString().PadLeft(3),
                barX,
                textY,
                Context.TIMS().Format18,
                Context.CommonBrush
            );
        }

        private void DrawBcpTable()
        {
            var formationSpec = ViewModel.FormationSpec.Value;
            if (formationSpec == null) return;
            DrawTableBorder();
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "800", -10, 0, Context.TIMS().Format18,
                Context.CommonBrush, 1, 0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "400", -10, TableHeight / 2,
                Context.TIMS().Format18, Context.CommonBrush, 1, 0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "常·非BC压", -10, TableHeight,
                Context.TIMS().Format18, Context.CommonBrush, 1, 1);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "(KPa)", -10, TableHeight + 8,
                Context.TIMS().Format18, Context.CommonBrush, 1);

            var currentX = 0f;
            for (var col = 0; col < formationSpec.CarCount; col++)
            {
                var carIdx = _spec.GetCarIndex(formationSpec.CarCount, col);
                var isDoubleUnit = formationSpec.CarCount < 10 &&
                                   (formationSpec[carIdx].CarType == TIMSCarType.FirstCar ||
                                    formationSpec[carIdx].CarType == TIMSCarType.LastCar);
                var carW = isDoubleUnit ? _unitWidth * 2 + 1 : _unitWidth;
                if (isDoubleUnit)
                {
                    var leftBarX = (float)Math.Floor(currentX + carW / 4f) + 0.5f - 8f;
                    var rightBarX = (float)Math.Floor(currentX + 3f * carW / 4f) + 0.5f - 8f;
                    var leftBcp = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.BcpList2[col]
                        : ViewModel.BcpList1[col];
                    var rightBcp = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                        ? ViewModel.BcpList1[col]
                        : ViewModel.BcpList2[col];

                    DrawBarAndText(leftBarX, leftBcp, 800, MonitorColors.White);
                    DrawBarAndText(rightBarX, rightBcp, 800, MonitorColors.White);
                }
                else
                {
                    var barX = (float)Math.Floor(currentX + carW / 2f) + 0.5f - 8f;
                    var bcp = ViewModel.BcpList1[col];

                    DrawBarAndText(barX, bcp, 800, MonitorColors.White);
                }

                currentX += carW + 1;
            }
        }

        private void DrawRegenerationTable()
        {
            var formationSpec = ViewModel.FormationSpec.Value;
            if (formationSpec == null) return;

            DrawTableBorder();
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "100", -10, 0, Context.TIMS().Format18,
                Context.CommonBrush, 1, 0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "50", -10, TableHeight / 2,
                Context.TIMS().Format18, Context.CommonBrush, 1, 0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "0", -10, TableHeight,
                Context.TIMS().Format18, Context.CommonBrush, 1, 1);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "回生PTN(KN)", -10, TableHeight + 8,
                Context.TIMS().Format18, Context.CommonBrush, 1);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "ﾌｨｰﾄﾞﾊﾞｯｸ", -10, TableHeight + 8 + 20,
                Context.TIMS().Format18, Context.CommonBrush, 1);

            var currentX = 0f;

            for (var col = 0; col < formationSpec.CarCount; col++)
            {
                var carIdx = _spec.GetCarIndex(formationSpec.CarCount, col);
                var isDoubleUnit = formationSpec.CarCount < 10 &&
                                   (formationSpec[carIdx].CarType == TIMSCarType.FirstCar ||
                                    formationSpec[carIdx].CarType == TIMSCarType.LastCar);
                var carW = isDoubleUnit ? _unitWidth * 2 + 1 : _unitWidth;

                if (formationSpec[carIdx].CarType == TIMSCarType.MotorCar)
                {
                    var motorForceFeedback = ViewModel.MotorForceFeedbackList[col];
                    var airBrakeForce = ViewModel.AirBrakeForceList[col];
                    var notch = ViewModel.NotchList[col];
                    const float totalGroupWidth = 36f;
                    var startX = (float)Math.Floor(currentX + (carW - totalGroupWidth) / 2f) + 0.5f;
                    var bar1X = startX;
                    var bar2X = startX + 20f;
                    DrawBarAndText(bar1X, notch >= -8 ? motorForceFeedback + airBrakeForce : 0, 100,
                        MonitorColors.White);
                    DrawBarAndText(bar2X, motorForceFeedback, 100, D01AXCarStateGroup.PoweringColor,
                        TableHeight + 8 + 20);
                }

                currentX += carW + 1;
            }
        }

        private void DrawTableBorder()
        {
            Context.CommonBrush.Color = MonitorColors.TIMSTitleGrey;
            Context.DeviceContext.DrawLine(new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, TableHeight + 0.5f),
                Context.CommonBrush);
            Context.DeviceContext.DrawLine(new Vector2(0.5f, 0.5f), new Vector2(7.5f, 0.5f),
                Context.CommonBrush);
            Context.DeviceContext.DrawLine(new Vector2(0.5f, TableHeight / 2 + 0.5f),
                new Vector2(7.5f, TableHeight / 2 + 0.5f),
                Context.CommonBrush);
            Context.DeviceContext.DrawLine(new Vector2(0.5f, TableHeight + 0.5f),
                new Vector2(_totalWidth - 0.5f, TableHeight + 0.5f),
                Context.CommonBrush);
        }

        protected struct FooterItemConfig
        {
            public readonly string Name;
            public readonly IValueSignal<RichTextDocument> Text;

            public FooterItemConfig(string name, IValueSignal<RichTextDocument> text)
            {
                Name = name;
                Text = text;
            }
        }
    }

    public class D05ABDataTableViewModel : D05ABViewModelBase
    {
        private TickTracker _brakeTickTracker;
        private TickTracker _normalTickTracker;
        private IPanelDataProvider _panelDataProvider;
        private IVehicleStateProvider _vehicleStateProvider;

        public D05ABDataTableViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            BcpList1 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            BcpList2 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            NotchList = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            MotorForceFeedbackList = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            AirBrakeForceList = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
        }

        public Signal<bool> DirectAirBackupBrake { get; } = new Signal<bool>();
        public Signal<bool> SnowBrake { get; } = new Signal<bool>();
        public Signal<int> Notch { get; } = new Signal<int>();
        public Signal<int> TascBrake { get; } = new Signal<int>();
        public ReactiveArray<int> BcpList1 { get; }
        public ReactiveArray<int> BcpList2 { get; }
        public ReactiveArray<int> NotchList { get; }
        public ReactiveArray<int> AirBrakeForceList { get; }
        public ReactiveArray<int> MotorForceFeedbackList { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _panelDataProvider = dataHub.Get<IPanelDataProvider>();
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
            TickTrackers.Add(_normalTickTracker);
            _brakeTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Brake));
            TickTrackers.Add(_brakeTickTracker);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            _normalTickTracker.Sync();
            if (!_brakeTickTracker.TrackAndSync()) return;
            Notch.Value =
                TIMSHelper.GetNotchValue(_vehicleStateProvider.PowerNotch, _vehicleStateProvider.BrakeNotch);
            TascBrake.Value = _vehicleStateProvider.TascBrakeNotch;
            DirectAirBackupBrake.Value = _panelDataProvider.IsActive(DirectInputIds.DirectAirBackupBrake);
            SnowBrake.Value = _panelDataProvider.IsActive(DirectInputIds.SnowBrake);
        }

        protected override void OnUpdateCarState(int col, ICarState carState)
        {
            if (FormationSpec.Value == null || col >= FormationSpec.Value.CarCount) return;
            if (_normalTickTracker.ShouldTrigger)
            {
                var carCount = FormationSpec.Value.CarCount;
                var carIdx = Spec.GetCarIndex(carCount, col);
                var isMotor = FormationSpec.Value[carIdx].CarType == TIMSCarType.MotorCar;
                if (isMotor)
                {
                    BcpList1[col] = TIMSHelper.GetImpreciseValue(carState.MotorCarBcPressure, 10);
                    BcpList2[col] = TIMSHelper.GetImpreciseValue(carState.MotorCarBcPressure2, 10);
                }
                else
                {
                    BcpList1[col] = TIMSHelper.GetImpreciseValue(carState.TrailerCarBcPressure, 10);
                    BcpList2[col] = TIMSHelper.GetImpreciseValue(carState.TrailerCarBcPressure2, 10);
                }

                MotorForceFeedbackList[col] = TIMSHelper.GetImpreciseValue(Math.Max(0, -carState.MotorForceFeedback));
                AirBrakeForceList[col] = TIMSHelper.GetImpreciseValue(carState.MotorAirBrakeForce);
            }

            if (_brakeTickTracker.ShouldTrigger)
                NotchList[col] = TIMSHelper.GetNotchValue(carState.PowerNotch, carState.BrakeNotch);
        }
    }

    public class D05ABDataTable1000 : D05ABDataTable
    {
        public D05ABDataTable1000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec,
            new D05ABDataTable1000ViewModel(spec))
        {
        }

        protected new D05ABDataTable1000ViewModel ViewModel => (D05ABDataTable1000ViewModel)base.ViewModel;

        protected override void AddFooterItemConfigs()
        {
            AddBoolFooterItemConfig("ATC転動防止Ｂ", ViewModel.IsAtcHoldingBrakeActive);
        }
    }

    public class D05ABDataTable1000ViewModel : D05ABDataTableViewModel
    {
        public D05ABDataTable1000ViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            var normalAtcViewModel = new NormalAtcViewModel();
            IsAtcHoldingBrakeActive = normalAtcViewModel.IsAtcHoldingBrakeActive;
            AddSubViewModel(normalAtcViewModel);
        }

        public Signal<bool> IsAtcHoldingBrakeActive { get; }
    }
}