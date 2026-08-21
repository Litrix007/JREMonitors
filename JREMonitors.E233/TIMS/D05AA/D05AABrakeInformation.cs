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
using JREMonitors.E233.ViewModels;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;

namespace JREMonitors.E233.TIMS.D05AA
{
    public abstract class D05AABrakeInformation : Widget<D05AABrakeInformationViewModel>
    {
        private const float StartY = 20;
        private const float Spacing = 22;

        protected readonly List<ItemConfig> ItemsConfigs = new List<ItemConfig>();

        public D05AABrakeInformation(RenderContext context, TIMSVehicleSpec spec,
            D05AABrakeInformationViewModel viewModel) : base(context,
            y: 160)
        {
            ViewModel = viewModel;
            FirstCarX = CreateRelayPropertySlot<float>();
            ItemsConfigs.Add(new ItemConfig("運転台", null));
            ItemsConfigs.Add(new ItemConfig("直予備Ｂ", ViewModel.DirectAirBackupBrake));
            ItemsConfigs.Add(new ItemConfig("ＥＢ", ViewModel.Eb));
            AddItems();
            ItemsConfigs.Add(new ItemConfig("車掌非常", new Signal<bool>()));
            ItemsConfigs.Add(new ItemConfig("ＢＨ非常", new Signal<bool>()));
            ItemsConfigs.Add(new ItemConfig("駐車元ダメ圧", new Signal<bool>()));
            ItemsConfigs.Add(new ItemConfig("駐車ブレーキ", ViewModel.SpringBrake));
            ItemsConfigs.Add(new ItemConfig("駐車Ｂ緩解", new Signal<bool>()));
            ItemsConfigs.Add(new ItemConfig("移動禁止", new Signal<bool>()));
            for (var i = 0; i < ItemsConfigs.Count; i++)
            {
                var item = new Item(context, spec, ViewModel.VehicleDirection, ViewModel.FormationSpec,
                    ItemsConfigs[i]);
                item.X.Bind(FirstCarX);
                item.Y.Value = i * Spacing + StartY;
                AddChild(item);
            }

            var hintRow = new Row(context, 400, 265,
                widgets: new List<Widget>
                {
                    new TIMSCircle(context, Colors.Red),
                    new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("：ブレーキ\u3000"),
                        contentColor: MonitorColors.TIMSTitleGrey),
                    new TIMSCircle(context),
                    new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("：定位"),
                        contentColor: MonitorColors.TIMSTitleGrey)
                }, positionSnapToPixels: true,
                rowHorizontalAlignment: 0.5f, widgetVerticalAlignment: 0.5f);
            AddChild(hintRow);
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public PropertySlot<float> FirstCarX { get; }
        public abstract void AddItems();

        protected struct ItemConfig
        {
            public string Name { get; }
            public IValueSignal<bool> Applied { get; }

            public ItemConfig(string name, IValueSignal<bool> applied)
            {
                Name = name;
                Applied = applied;
            }
        }

        private class Item : Widget
        {
            private const float PointY1 = 10.5f;
            private readonly IValueSignal<bool> _applied;
            private readonly Baker _baker;
            private readonly IValueSignal<TIMSFormationSpec> _formationSpec;
            private readonly ReactiveList<int> _lineIndices;
            private readonly float _startX;
            private readonly float _unitWidth;
            private readonly IValueSignal<TIMSVehicleDirection> _vehicleDirection;

            public Item(
                RenderContext context,
                TIMSVehicleSpec spec,
                IValueSignal<TIMSVehicleDirection> vehicleDirectionSource,
                IValueSignal<TIMSFormationSpec> formationSpecSource,
                ItemConfig config
            ) : base(context)
            {
                var spec1 = spec;
                _unitWidth = TIMSCarGroup.GetUnitWidth(spec);
                _startX = _unitWidth / 2;
                _applied = config.Applied;
                _vehicleDirection = vehicleDirectionSource;
                _formationSpec = formationSpecSource;
                AddChild(new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(config.Name, horizontalAlignment: 1, verticalAlignment: 0.5f),
                    targetBounds: new RectangleF(0, 0, 0, Spacing),
                    contentColor: MonitorColors.White, x: -2));
                _lineIndices = CreateRelayReactiveList<int>();
                WatchEffect(EffectPhase.State, () =>
                {
                    var formationSpec = _formationSpec.Value;
                    if (formationSpec == null) return;
                    _lineIndices.Clear();
                    var start = -1;
                    for (var i = 0; i < formationSpec.CarCount; i++)
                    {
                        var carIdx = spec1.GetCarIndex(formationSpec.CarCount, i);
                        if (formationSpec[carIdx].CarType == TIMSCarType.FirstCar ||
                            formationSpec[carIdx].CarType == TIMSCarType.LastCar)
                        {
                            if (start < 0)
                            {
                                start = i;
                                continue;
                            }

                            _lineIndices.Add(start);
                            _lineIndices.Add(i);
                            start = -1;
                        }
                    }
                });
                _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_baker);
                if (config.Applied == null)
                {
                    var texts = new Signal<string>[4];
                    var textWidgets = new BoundsDrawerWidget[4];
                    for (var i = 0; i < 4; i++)
                    {
                        var j = i;
                        texts[j] = new Signal<string>();
                        textWidgets[j] = new BoundsDrawerWidget(context,
                            this.CreateTIMSTextDrawer(
                                CreateComputed(() => RichTextParser.Raw(texts[j])),
                                horizontalAlignment: 0.5f, verticalAlignment: 0.5f),
                            targetBounds: new RectangleF(0, 0, 0, Spacing),
                            backgroundColor: Colors.Black,
                            contentColor: MonitorColors.White);
                        AddChild(textWidgets[i]);
                    }

                    WatchEffect(EffectPhase.State, () =>
                    {
                        var textIndex = 0;
                        for (var i = 0; i < _lineIndices.Count; i++)
                        {
                            var idx = _lineIndices[i];
                            texts[textIndex].Value = i == 0
                                ? vehicleDirectionSource.Value == TIMSVehicleDirection.Left ? "前" : "後"
                                : i == _lineIndices.Count - 1
                                    ? vehicleDirectionSource.Value == TIMSVehicleDirection.Right ? "前" : "後"
                                    : "中";
                            textWidgets[textIndex].IsVisible.Value = true;
                            textWidgets[textIndex].X.Value = _startX + idx * (_unitWidth + 1);
                            textIndex++;
                        }

                        for (var i = textIndex; i < 4; i++) textWidgets[i].IsVisible.Value = false;
                    });
                }
                else
                {
                    WatchEffect(() => _baker.Refresh(), config.Applied);
                }

                WatchEffect(() => _baker.Refresh(), _vehicleDirection, _formationSpec);
            }

            public override RectangleF SelfRelativeDirtyBounds
            {
                get
                {
                    var formationSpec = _formationSpec.Value;
                    return formationSpec == null
                        ? RectangleF.Empty
                        : new RectangleF(0, 0, formationSpec.CarCount * _unitWidth + (formationSpec.CarCount - 1), 19);
                }
            }

            protected override void OnDraw(float totalScale)
            {
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, () =>
                {
                    var formationSpec = _formationSpec.Value;
                    if (formationSpec == null) return;
                    var applyIndex = _vehicleDirection.Value == TIMSVehicleDirection.Left
                        ? 0
                        : formationSpec.CarCount - 1;
                    var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                    Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                    for (var i = 0; i + 1 < _lineIndices.Count; i += 2)
                    {
                        Context.CommonBrush.Color = MonitorColors.White;
                        var startIdx = _lineIndices[i];
                        var endIdx = _lineIndices[i + 1];
                        var startPoint = new Vector2(_startX + 0.5f + startIdx * (_unitWidth + 1), PointY1);
                        var endPoint = new Vector2(_startX + 0.5f + endIdx * (_unitWidth + 1), PointY1);
                        Context.DeviceContext.DrawLine(startPoint, endPoint, Context.CommonBrush);
                        if (_applied == null) continue;
                        Context.CommonBrush.Color = _applied.Value && startIdx == applyIndex
                            ? Colors.Red
                            : MonitorColors.White;
                        Context.DeviceContext.FillEllipse(
                            new Ellipse(startPoint, TIMSCircle.CircleRadius, TIMSCircle.CircleRadius),
                            Context.CommonBrush);
                        Context.CommonBrush.Color = _applied.Value && endIdx == applyIndex
                            ? Colors.Red
                            : MonitorColors.White;
                        Context.DeviceContext.FillEllipse(
                            new Ellipse(endPoint, TIMSCircle.CircleRadius, TIMSCircle.CircleRadius),
                            Context.CommonBrush);
                    }

                    Context.DeviceContext.AntialiasMode = oldAntialiasMode;
                });
            }
        }
    }

    public class D05AABrakeInformationViewModel : TIMSFormationViewModel
    {
        private TickTracker _normalTickTracker;
        protected DelayService DelayService;
        protected IPanelDataProvider PanelDataProvider;
        protected IVehicleStateProvider VehicleStateProvider;

        public D05AABrakeInformationViewModel(TIMSVehicleSpec spec) : base(spec)
        {
        }

        public Signal<bool> Eb { get; } = new Signal<bool>();
        public Signal<bool> DirectAirBackupBrake { get; } = new Signal<bool>();
        public Signal<bool> SpringBrake { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            PanelDataProvider = dataHub.Get<IPanelDataProvider>();
            VehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            DelayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(DelayService.GetDelayProvider(DelayTypes.Normal));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            if (_normalTickTracker.TrackAndSync())
            {
                Eb.Value = VehicleStateProvider.BrakeNotch > 8;
                DirectAirBackupBrake.Value = PanelDataProvider.IsActive(DirectInputIds.DirectAirBackupBrake);
                SpringBrake.Value = PanelDataProvider.IsActive(DirectInputIds.SpringBrake);
            }
        }

        protected override void OnReset()
        {
            base.OnReset();
            _normalTickTracker.Reset();
        }
    }

    public class D05AABrakeInformation0 : D05AABrakeInformation
    {
        public D05AABrakeInformation0(RenderContext context, TIMSVehicleSpec spec) : base(context, spec,
            new D05AABrakeInformation0ViewModel(spec))
        {
        }

        private new D05AABrakeInformation0ViewModel ViewModel => (D05AABrakeInformation0ViewModel)base.ViewModel;

        public override void AddItems()
        {
            ItemsConfigs.Add(new ItemConfig("ＡＴＳ-Ｐ", ViewModel.AtsP));
            ItemsConfigs.Add(new ItemConfig("ＡＴＳ-ＳＮ", ViewModel.AtsSn));
        }
    }

    public class D05AABrakeInformation0ViewModel : D05AABrakeInformationViewModel
    {
        public D05AABrakeInformation0ViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            var atsStateViewModel = new AtsStateViewModel();
            AtsSn = atsStateViewModel.IsAtsSActivatedLit;
            AtsP = CreateComputed(() =>
                atsStateViewModel.IsAtsPServiceBrakeLit || atsStateViewModel.IsAtsPEmergencyBrakeLit);
            AddSubViewModel(atsStateViewModel);
        }

        public Signal<bool> AtsSn { get; }
        public Computed<bool> AtsP { get; }
    }

    public class D05AABrakeInformation1000 : D05AABrakeInformation
    {
        public D05AABrakeInformation1000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec,
            new D05AABrakeInformation1000ViewModel(spec))
        {
        }

        private new D05AABrakeInformation1000ViewModel ViewModel => (D05AABrakeInformation1000ViewModel)base.ViewModel;

        public override void AddItems()
        {
            ItemsConfigs.Add(new ItemConfig("ＡＴＣ", ViewModel.Atc));
        }
    }

    public class D05AABrakeInformation1000ViewModel : D05AABrakeInformationViewModel
    {
        public D05AABrakeInformation1000ViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            var normalAtcViewModel = new NormalAtcViewModel();
            Atc = CreateComputed(() =>
                normalAtcViewModel.IsAtcServiceBrakeLit || normalAtcViewModel.IsAtcEmergencyBrakeLit);
            AddSubViewModel(normalAtcViewModel);
        }

        public Computed<bool> Atc { get; }
    }
}