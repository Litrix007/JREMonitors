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
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Services.Car;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D02AA
{
    public class D02AAEquipmentInformation : Widget<D02AAEquipmentInformationViewModel>
    {
        private const float TextStartY = 20;
        private const float TextHeight = 20;
        private const float Spacing = 22;

        private static readonly string[] Titles =
        {
            "マスコン",
            "ＶＶＶＦ",
            "ブレ一キ",
            "\u3000\u3000（後）",
            "ＳＩＶ",
            "コンプレッサ",
            "Ｉ／Ｆ",
            "ＴＩＭＳ"
        };

        private readonly Baker _baker;
        private readonly float _carWidth;
        private readonly TIMSVehicleSpec _spec;

        public D02AAEquipmentInformation(RenderContext context, TIMSVehicleSpec spec) : base(context, y: 250)
        {
            _spec = spec;
            _carWidth = TIMSCarGroup.GetUnitWidth(spec);
            ViewModel = new D02AAEquipmentInformationViewModel(spec);
            FirstCarX = CreatePropertySlot<float>(DirtyType.Visual);
            _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
            RegisterResource(_baker);
            AddTitleWidgets(context);
            var bcpTextList1 = new BoundsDrawerWidget[TIMSFormationSpec.MaxCarCount];
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                var j = i;
                bcpTextList1[j] = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(
                        CreateComputed(() => RichTextParser.Raw(ViewModel.BcpList1[j].ToString().PadLeft(4))),
                        horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                        cache: false
                    ),
                    targetBounds: new RectangleF(-20, 0, 40, 20),
                    useBaker: true,
                    contentColor: MonitorColors.White, backgroundColor: Colors.Black, strokeColor: MonitorColors.White,
                    strokeWidth: 1, backgroundAntialiasMode: AntialiasMode.Aliased, y: TextStartY + Spacing * 2);
                AddChild(bcpTextList1[i]);
            }

            var bcpTextList2 = new BoundsDrawerWidget[4];
            var bcp2Indices = new Signal<int>[4];
            for (var i = 0; i < 4; i++)
            {
                var j = i;
                bcp2Indices[j] = new Signal<int>();
                bcpTextList2[j] = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(
                        CreateComputed(() =>
                            RichTextParser.Raw(ViewModel.BcpList2[bcp2Indices[j]].ToString().PadLeft(4))),
                        horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                        cache: false
                    ),
                    targetBounds: new RectangleF(-20, 0, 40, 20),
                    useBaker: true,
                    contentColor: MonitorColors.White, backgroundColor: Colors.Black, strokeColor: MonitorColors.White,
                    strokeWidth: 1, backgroundAntialiasMode: AntialiasMode.Aliased, y: TextStartY + Spacing * 3);
                AddChild(bcpTextList2[i]);
            }

            var compressorIndices = new Signal<int>[TIMSFormationSpec.MaxCarCount];
            var isFinalCompressorList = new Signal<bool>[TIMSFormationSpec.MaxCarCount];
            var compressorTextList = new BoundsDrawerWidget[TIMSFormationSpec.MaxCarCount];
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                var j = i;
                compressorIndices[j] = new Signal<int>();
                isFinalCompressorList[j] = new Signal<bool>();
                compressorTextList[j] = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(
                        CreateComputed(() =>
                            RichTextParser.Raw(ViewModel.IsCompressorWorkingList[compressorIndices[j]] ? "動作" :
                                isFinalCompressorList[j] ? "休止" : "停止")),
                        horizontalAlignment: 0.5f, verticalAlignment: 0.5f
                    ),
                    targetBounds: new RectangleF(-20, 0, 40, 20),
                    useBaker: true,
                    contentColor: MonitorColors.White, backgroundColor: Colors.Black, strokeColor: MonitorColors.White,
                    strokeWidth: 1, backgroundAntialiasMode: AntialiasMode.Aliased, y: TextStartY + Spacing * 5);
                AddChild(compressorTextList[i]);
            }

            var hintRow = new Row(context, 400, 210,
                widgets: new List<Widget>
                {
                    new TIMSCircle(context),
                    new BoundsDrawerWidget(context,
                        this.CreateTIMSTextDrawer("：正常\u3000"),
                        contentColor: MonitorColors.TIMSTitleGrey),
                    new TIMSCircle(context, Colors.Aqua),
                    new BoundsDrawerWidget(context,
                        this.CreateTIMSTextDrawer("：機器情報有\u3000"),
                        contentColor: MonitorColors.TIMSTitleGrey),
                    new TIMSCircle(context, Colors.Red),
                    new BoundsDrawerWidget(context,
                        this.CreateTIMSTextDrawer("：異常\u3000"),
                        contentColor: MonitorColors.TIMSTitleGrey),
                    new BoundsDrawerWidget(context,
                        this.CreateTIMSTextDrawer(" XXX", verticalAlignment: 0.5f),
                        targetBounds: new RectangleF(0, 0, 1, 20),
                        backgroundColor: Colors.Aqua,
                        contentColor: Colors.Black,
                        strokeColor: MonitorColors.TIMSTitleGrey,
                        strokeWidth: 1,
                        useBaker: true,
                        bakerPrescaleMode: BakerPrescaleMode.AutoCubic,
                        backgroundAntialiasMode: AntialiasMode.Aliased),
                    new BoundsDrawerWidget(context,
                        this.CreateTIMSTextDrawer("：機器情報有"),
                        contentColor: MonitorColors.TIMSTitleGrey)
                }, positionSnapToPixels: true,
                rowHorizontalAlignment: 0.5f, widgetVerticalAlignment: 0.5f, rowVerticalAlignment: 0.5f);
            AddChild(hintRow);
            WatchEffect(EffectPhase.Visual, () =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec == null) return;
                var startX = StartX;

                for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
                    if (i >= formationSpec.CarCount)
                    {
                        bcpTextList1[i].IsVisible.Value = false;
                    }
                    else
                    {
                        bcpTextList1[i].IsVisible.Value = true;
                        bcpTextList1[i].X.Value = startX + i * (_carWidth + 1);
                    }

                var bcp2Index = 0;
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                    if (formationSpec[carIdx].CarType == TIMSCarType.FirstCar ||
                        formationSpec[carIdx].CarType == TIMSCarType.LastCar)
                    {
                        bcpTextList2[bcp2Index].IsVisible.Value = true;
                        bcpTextList2[bcp2Index].X.Value = startX + i * (_carWidth + 1);
                        bcp2Indices[bcp2Index].Value = i;
                        bcp2Index++;
                    }
                }

                for (var i = bcp2Index; i < 4; i++) bcpTextList2[i].IsVisible.Value = false;

                var firstCpScreenIndex = -1;
                if (ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Left)
                    for (var i = 0; i < formationSpec.CarCount; i++)
                    {
                        var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                        if (formationSpec[carIdx].HasCompressor)
                        {
                            firstCpScreenIndex = i;
                            break;
                        }
                    }
                else
                    for (var i = formationSpec.CarCount - 1; i >= 0; i--)
                    {
                        var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                        if (formationSpec[carIdx].HasCompressor)
                        {
                            firstCpScreenIndex = i;
                            break;
                        }
                    }

                var compressorIndex = 0;
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                    if (formationSpec[carIdx].HasCompressor)
                    {
                        compressorTextList[compressorIndex].IsVisible.Value = true;
                        compressorTextList[compressorIndex].X.Value = startX + i * (_carWidth + 1);
                        compressorIndices[compressorIndex].Value = i;
                        isFinalCompressorList[compressorIndex].Value = i == firstCpScreenIndex;
                        compressorIndex++;
                    }
                }

                for (var i = compressorIndex; i < TIMSFormationSpec.MaxCarCount; i++)
                    compressorTextList[i].IsVisible.Value = false;
            });
            WatchEffect(() => { _baker.Refresh(); }, ViewModel.FormationSpec, FirstCarX);
        }


        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, 800, 230);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public PropertySlot<float> FirstCarX { get; }

        private float StartX => (float)Math.Floor(FirstCarX + _carWidth / 2f);

        private void AddTitleWidgets(RenderContext context)
        {
            for (var i = 0; i < Titles.Length; i++) AddChild(CreateTitleWidget(context, Titles[i], i));
        }

        private BoundsDrawerWidget CreateTitleWidget(RenderContext context, string text, int rowIndex)
        {
            return new BoundsDrawerWidget(
                context,
                this.CreateTIMSTextDrawer(text, verticalAlignment: 0.5f),
                targetBounds: new RectangleF(0, 0, 0, TextHeight),
                contentColor: MonitorColors.White,
                x: 20,
                y: TextStartY + Spacing * rowIndex
            );
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, () =>
            {
                var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec == null)
                {
                    Context.DeviceContext.AntialiasMode = oldAntialiasMode;
                    return;
                }

                Context.CommonBrush.Color = MonitorColors.White;
                var startX = StartX + 0.5f;
                const float startY = TextStartY + 9 + 0.5f;
                var endX = GetX(formationSpec.CarCount - 1);
                const float endY = startY + Spacing * 7;
                for (var i = 0; i < 8; i++)
                {
                    var y = startY + Spacing * i;
                    Context.DeviceContext.DrawLine(
                        new Vector2(startX, y),
                        new Vector2(endX, y),
                        Context.CommonBrush);
                }

                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var x = startX + i * (_carWidth + 1);
                    Context.DeviceContext.DrawLine(
                        new Vector2(x, startY),
                        new Vector2(x, endY),
                        Context.CommonBrush);
                }

                int GetIndex(int i)
                {
                    return _spec.GetCarIndex(formationSpec.CarCount, i);
                }

                // マスコン
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var idx = GetIndex(i);
                    var x = GetX(i);
                    if (formationSpec[idx].CarType != TIMSCarType.FirstCar &&
                        formationSpec[idx].CarType != TIMSCarType.LastCar) continue;
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(x, startY), TIMSCircle.CircleRadius, TIMSCircle.CircleRadius),
                        Context.CommonBrush);
                }

                // VVVF
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var idx = GetIndex(i);
                    var x = GetX(i);
                    if (formationSpec[idx].CarType != TIMSCarType.MotorCar) continue;
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(x, startY + Spacing), TIMSCircle.CircleRadius, TIMSCircle.CircleRadius),
                        Context.CommonBrush);
                }

                // SIV
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var idx = GetIndex(i);
                    var x = GetX(i);
                    if (!formationSpec[idx].HasSiv) continue;
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(x, startY + Spacing * 4), TIMSCircle.CircleRadius,
                            TIMSCircle.CircleRadius),
                        Context.CommonBrush);
                }

                // I/F + TIMS
                for (var i = 0; i < formationSpec.CarCount; i++)
                {
                    var x = GetX(i);
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(x, startY + Spacing * 6), TIMSCircle.CircleRadius,
                            TIMSCircle.CircleRadius),
                        Context.CommonBrush);
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(x, startY + Spacing * 7), TIMSCircle.CircleRadius,
                            TIMSCircle.CircleRadius),
                        Context.CommonBrush);
                }

                Context.DeviceContext.AntialiasMode = oldAntialiasMode;
                return;

                float GetX(int i)
                {
                    return startX + i * (_carWidth + 1);
                }
            });
        }
    }

    public class D02AAEquipmentInformationViewModel : TIMSFormationViewModel
    {
        private CarStateService _carStateService;
        private TickTracker _normalTickTracker;

        public D02AAEquipmentInformationViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            BcpList1 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            BcpList2 = CreateReactiveArray<int>(TIMSFormationSpec.MaxCarCount);
            IsCompressorWorkingList = CreateReactiveArray<bool>(TIMSFormationSpec.MaxCarCount);
        }

        public ReactiveArray<int> BcpList1 { get; }
        public ReactiveArray<int> BcpList2 { get; }
        public ReactiveArray<bool> IsCompressorWorkingList { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
            _carStateService = dataHub.Get<CarStateService>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (IsVehicleDirectionChanged || IsFormationSpecChanged) _normalTickTracker.Reset();
            if (!_normalTickTracker.TrackAndSync()) return;
            var formationSpec = FormationSpec.Value;
            if (formationSpec == null) return;
            var carCount = formationSpec.CarCount;
            var vehicleDirection = VehicleDirection.Value;
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                if (i >= carCount)
                {
                    BcpList1[i] = 0;
                    BcpList2[i] = 0;
                    IsCompressorWorkingList[i] = false;
                    continue;
                }

                var carIdx = Spec.GetCarIndex(carCount, i);
                var isMotorCar = formationSpec[carIdx].CarType == TIMSCarType.MotorCar;
                var dataIdx = TIMSVehicleSpec.GetDataIndex(carCount, vehicleDirection, i);
                var carState = _carStateService.GetCarStateAt(dataIdx);
                if (carState == null) continue;
                if (isMotorCar)
                {
                    BcpList1[i] = TIMSHelper.GetImpreciseValue(carState.MotorCarBcPressure, 10);
                    BcpList2[i] = TIMSHelper.GetImpreciseValue(carState.MotorCarBcPressure2, 10);
                }
                else
                {
                    BcpList1[i] = TIMSHelper.GetImpreciseValue(carState.TrailerCarBcPressure, 10);
                    BcpList2[i] = TIMSHelper.GetImpreciseValue(carState.TrailerCarBcPressure2, 10);
                }

                IsCompressorWorkingList[i] = carState.IsCompressorWorking;
            }
        }

        protected override void OnReset()
        {
            _normalTickTracker.Reset();
        }
    }
}