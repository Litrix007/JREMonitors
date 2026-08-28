using System;
using System.Drawing;
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
using JREMonitors.JRE.Constants;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenSlowSectionInfo : Widget<D01AXEDenSlowSectionInfoViewModel>
    {
        private const float BaseWidth = 390;
        private const float Height = 52;
        private readonly float _width;

        public D01AXEDenSlowSectionInfo(RenderContext context, RenderContext scopedContext, TIMSVehicleSpec spec) :
            base(context, y: 310)
        {
            ViewModel = new D01AXEDenSlowSectionInfoViewModel();
            IsVisible.Bind(ViewModel.IsVisible);
            _width = BaseWidth + (spec.D01AXSpec.HideNextDutyBackgroundWhenEmpty ? 22 : 0);
            var paddingRight = spec.D01AXSpec.HideNextDutyBackgroundWhenEmpty ? 0 : 30;
            X.Bind(CreateComputed(() =>
                ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Right
                    ? spec.D01AXSpec.MayShowRouteSetInformation ? 137 : 30
                    : 800 - paddingRight - _width));
            var title = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("徐行", horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                    useVerticalOverhangMetrics: true),
                targetBounds: new RawRectF(0, 0, 40, Height - 12),
                backgroundAntialiasMode: AntialiasMode.Aliased
            );
            title.BackgroundColor.Bind(CreateComputed(() =>
                ViewModel.IsBlink ? Colors.Yellow : Colors.Yellow.WithAlpha(0.2f)));
            title.ContentColor.Bind(CreateComputed(() => ViewModel.IsBlink ? Colors.Black : Colors.Yellow));
            var section1 = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.Section1.Value)),
                    context: scopedContext,
                    horizontalAlignment: 0.5f
                ),
                contentColor: Colors.White);
            section1.CustomPreferredWidth.Value = LayoutLength.Flex();
            section1.CustomPreferredHeight.Value = LayoutLength.Absolute(20);
            var section2 = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.Section2.Value)),
                    context: scopedContext,
                    horizontalAlignment: 0.5f
                ),
                contentColor: Colors.White);
            section2.CustomPreferredWidth.Value = LayoutLength.Flex();
            section2.CustomPreferredHeight.Value = LayoutLength.Absolute(20);
            var sectionCol = new Col(context, widgets: new Widget[] { section1, section2 }, positionSnapToPixels: true);
            var limit1 = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.SpeedLimit1.Value)),
                    horizontalAlignment: 1, context: scopedContext
                ),
                backgroundAntialiasMode: AntialiasMode.Aliased);
            limit1.BackgroundColor.Bind(CreateComputed(() =>
                ViewModel.IsBlink ? Colors.Yellow : Colors.Yellow.WithAlpha(0.2f)));
            limit1.ContentColor.Bind(CreateComputed(() => ViewModel.IsBlink ? Colors.Black : Colors.Yellow));
            limit1.CustomPreferredHeight.Value = LayoutLength.Absolute(20);
            var limit2 = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.SpeedLimit2.Value)),
                    horizontalAlignment: 1, context: scopedContext
                ),
                backgroundAntialiasMode: AntialiasMode.Aliased);
            limit2.BackgroundColor.Bind(CreateComputed(() =>
            {
                if (string.IsNullOrEmpty(ViewModel.SpeedLimit2)) return Colors.Yellow;
                return ViewModel.IsBlink ? Colors.Yellow : Colors.Yellow.WithAlpha(0.2f);
            }));
            limit2.CustomPreferredWidth.Value = LayoutLength.Flex();
            limit2.ContentColor.Bind(CreateComputed(() => ViewModel.IsBlink ? Colors.Black : Colors.Yellow));
            limit2.CustomPreferredHeight.Value = LayoutLength.Absolute(20);
            var limitCol = new Col(context, spreadWidthFlex: false, widgets: new Widget[] { limit1, limit2 },
                positionSnapToPixels: true);
            var row = new Row(context, y: 6, explicitAvailableWidth: _width,
                widgets: new Widget[] { title, sectionCol, limitCol }, positionSnapToPixels: true);
            AddChild(row);
        }

        protected override bool SkipUpdateWhenHidden => false;
        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, _width, Height);

        protected override void OnDraw(float totalScale)
        {
            Context.DeviceContext.WithAliasedIfNeeded(() =>
            {
                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
                Context.CommonBrush.Color = Colors.Yellow;
                Context.DeviceContext.FillRectangle(new RectangleF(0, 0, _width, 6), Context.CommonBrush);
                Context.DeviceContext.FillRectangle(new RectangleF(0, 46, _width, 6), Context.CommonBrush);
            });
        }
    }

    public class D01AXEDenSlowSectionInfoViewModel : TIMSViewModel
    {
        private readonly Signal<int?> _endMileage1 = new Signal<int?>();
        private readonly Signal<int?> _endMileage2 = new Signal<int?>();
        private readonly Signal<int?> _speedLimit1 = new Signal<int?>();
        private readonly Signal<int?> _speedLimit2 = new Signal<int?>();
        private readonly Signal<int?> _startMileage1 = new Signal<int?>();
        private readonly Signal<int?> _startMileage2 = new Signal<int?>();
        private TickTracker _blinkTracker;

        public D01AXEDenSlowSectionInfoViewModel()
        {
            Section1 = CreateComputed(() => GetSection(_startMileage1, _endMileage1));
            SpeedLimit1 = CreateComputed(() => GetLimit(_speedLimit1));
            Section2 = CreateComputed(() => GetSection(_startMileage2, _endMileage2));
            SpeedLimit2 = CreateComputed(() => GetLimit(_speedLimit2));
        }

        public Signal<bool> IsVisible { get; } = new Signal<bool>();
        public Signal<bool> IsBlink { get; } = new Signal<bool>();
        public Computed<string> Section1 { get; }
        public Computed<string> SpeedLimit1 { get; }
        public Computed<string> Section2 { get; }
        public Computed<string> SpeedLimit2 { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            var delayService = dataHub.Get<DelayService>();
            _blinkTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Blink));
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            var section1 = ICCardService.CurrentSlowSections.Item1;
            var section2 = ICCardService.CurrentSlowSections.Item2;
            IsVisible.Value = section1.HasValue || section2.HasValue;
            IsBlink.Value = TIMSHelper.IsBlink(_blinkTracker.Sync());
            if (section1.HasValue)
            {
                _startMileage1.Value = section1.Value.StartMileage;
                _endMileage1.Value = section1.Value.EndMileage;
                _speedLimit1.Value = section1.Value.SpeedLimit;
            }
            else
            {
                _startMileage1.Value = null;
                _endMileage1.Value = null;
                _speedLimit1.Value = null;
            }

            if (section2.HasValue)
            {
                _startMileage2.Value = section2.Value.StartMileage;
                _endMileage2.Value = section2.Value.EndMileage;
                _speedLimit2.Value = section2.Value.SpeedLimit;
            }
            else
            {
                _startMileage2.Value = null;
                _endMileage2.Value = null;
                _speedLimit2.Value = null;
            }
        }

        private static string GetSection(int? start, int? end)
        {
            return start.HasValue && end.HasValue
                ? $"{start / 1000.0:000.000} km ～ {end / 1000.0:000.000} km"
                : string.Empty;
        }

        private static string GetLimit(int? limit)
        {
            return limit.HasValue ? $"制限 {limit.Value:000}km/h" : string.Empty;
        }
    }
}