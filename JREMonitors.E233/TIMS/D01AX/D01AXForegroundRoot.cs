using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.State;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.D01AX.EDen;
using JREMonitors.E233.TIMS.D01AX.MDen;
using JREMonitors.E233.TIMS.ICCard;
using Vortice;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXForegroundRoot : TIMSCommonForegroundRoot<D01AXForegroundRootViewModel>
    {
        private static readonly PropertyKey ScopedBakerCacheKey =
            new PropertyKey($"{nameof(ScreenIds.D01AX)}{nameof(BakerCache)}");

        private static readonly PropertyKey ScopedTextMetricsCacheKey =
            new PropertyKey($"{nameof(ScreenIds.D01AX)}{nameof(TextMetricsCache)}");

        private static readonly PropertyKey ScopedBrushCacheKey =
            new PropertyKey($"{nameof(ScreenIds.D01AX)}{nameof(BrushCache)}");

        private readonly D01AXEDenGroup _eDenGroup;
        private readonly D01AXMDenGroup _mDenGroup;
        private readonly BakerCache _scopedBakerCache;
        private readonly BrushCache _scopedBrushCache;
        private readonly ScopedRenderContext _scopedContext;
        private readonly TextMetricsCache _scopedTextMetricsCache;
        private readonly TIMSVehicleSpec _spec;

        public D01AXForegroundRoot(RenderContext context, TIMSVehicleSpec spec,
            D01AXTrainTypeButtonGroup trainTypeButtonGroup) : this(context, new D01AXBackground(context), spec,
            trainTypeButtonGroup)
        {
        }

        private D01AXForegroundRoot(RenderContext context, D01AXBackground background, TIMSVehicleSpec spec,
            D01AXTrainTypeButtonGroup trainTypeButtonGroup) :
            base(context, ScreenIds.D01AX, "運転情報", background)
        {
            ViewModel = new D01AXForegroundRootViewModel();
            _scopedContext = new ScopedRenderContext(context);
            _scopedBakerCache = context.GetService<BakerCache>(ScopedBakerCacheKey);
            _scopedTextMetricsCache = context.GetService<TextMetricsCache>(ScopedTextMetricsCacheKey);
            _scopedBrushCache = context.GetService(ScopedBrushCacheKey, () => new BrushCache(context));
            _scopedContext.Properties[BakerCache.Key] = _scopedBakerCache;
            _scopedContext.Properties[TextMetricsCache.Key] = _scopedTextMetricsCache;
            _scopedContext.Properties[BrushCache.Key] = _scopedBrushCache;
            ScreenTitle.Id.Bind(ViewModel.Id);
            _spec = spec;
            background.TimetableBounds.Bind(CreateComputed<RectangleF>(() =>
                ViewModel.DisplayMode == TIMSDisplayMode.MDen
                    ? new RawRectF(0, 110, 799, 269)
                    : new RawRectF(_spec.D01AXSpec.MayShowRouteSetInformation ? 134 : 0, 132, 799, 304)));
            AddChild(new D01AXButtonGroup(context, trainTypeButtonGroup));
            AddChild(new D01AXBaseInfoGroup(context, _scopedContext));
            _mDenGroup = new D01AXMDenGroup(context, _scopedContext);
            _mDenGroup.IsVisible.Bind(CreateComputed(() => ViewModel.DisplayMode == TIMSDisplayMode.MDen));
            AddChild(_mDenGroup);
            _eDenGroup = new D01AXEDenGroup(context, _scopedContext, spec);
            _eDenGroup.IsVisible.Bind(CreateComputed(() => ViewModel.DisplayMode == TIMSDisplayMode.EDen));
            AddChild(_eDenGroup);
            AddChild(new D01AXCarStateGroup(context, spec,
                spec.D01AXSpec.MayShowRouteSetInformation && spec.D01AXSpec.AlignCarStateToRouteSetInformation
                    ? 134
                    : 30));
            WatchEffect(() =>
            {
                _scopedBakerCache.Version = ViewModel.Version;
                _scopedTextMetricsCache.Version = ViewModel.Version;
                _scopedBrushCache.Version = ViewModel.Version;
            });
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, ViewModel.DisplayMode, ViewModel.Formation);
        }
    }

    public class D01AXForegroundRootViewModel : TIMSCommonForegroundRootViewModel
    {
        private TIMSICCardService<E233SignalSystem> _icCardService;
        public Signal<string> Id { get; } = new Signal<string>(new string('\u3000', 5));
        public Signal<TIMSDisplayMode> DisplayMode { get; } = new Signal<TIMSDisplayMode>();
        public Signal<string> Formation { get; } = new Signal<string>(string.Empty);
        public Signal<long> Version { get; } = new Signal<long>();

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _icCardService = dataHub.Get<TIMSICCardService<E233SignalSystem>>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            Id.Value = _icCardService.CurrentDisplayMode == TIMSDisplayMode.MDen ? "D01AA" : "D01AB";
            DisplayMode.Value = _icCardService.CurrentDisplayMode;
            Formation.Value = _icCardService.CurrentFormation;
            Version.Value = _icCardService.Version;
        }
    }
}