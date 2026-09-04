using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenTimeCol : Widget<D01AXEDenTimeColStates>, ILayoutable
    {
        public D01AXEDenTimeCol(RenderContext context, ScopedRenderContext scopedContext,
            D01AXEDenTimeColStates states) : base(context)
        {
            ViewModel = states;
            IsVisible.Bind(states.IsVisible);

            var stationNameText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    this.CreateTIMSTextLayout(
                        documentSource: CreateComputed(() => RichTextParser.Raw(states.StationName.Value)),
                        orientation: ContentOrientation.Vertical,
                        arrangement: ContentArrangement.Step,
                        sizeLimit: 54,
                        fixedLineSpacing: 0,
                        useHorizontalOverhangMetrics: true,
                        context: scopedContext
                    ),
                    horizontalAlignment: 0.5f,
                    context: scopedContext
                ));
            stationNameText.ContentColor.Bind(states.StationColor);
            stationNameText.BackgroundColor.Bind(states.StationBackgroundColor);
            AddChild(stationNameText);
            // 文字固定
            var stationTaskText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.StationTask.Value)),
                    horizontalAlignment: 0.5f,
                    useHorizontalOverhangMetrics: true
                ),
                contentColor: Colors.Yellow,
                y: 61);
            AddChild(stationTaskText);

            var arrivalTimeText = new D01AXNormalTimeText(
                scopedContext,
                -18,
                85,
                false,
                states.ArrivalMinutes,
                states.ArrivalSeconds
            );
            AddChild(arrivalTimeText);
            // 文字固定
            var arrivalIndicatorText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.ArrivalIndicator.Value)),
                    horizontalAlignment: 0.5f,
                    useHorizontalOverhangMetrics: true
                ),
                contentColor: MonitorColors.White,
                y: 85);
            AddChild(arrivalIndicatorText);
            var hasArrivalIndicator = CreateComputed(() => !string.IsNullOrEmpty(states.ArrivalIndicator.Value));
            arrivalIndicatorText.IsVisible.Bind(hasArrivalIndicator);
            arrivalTimeText.IsVisible.Bind(CreateComputed(() => !hasArrivalIndicator.Value));
            var departureTimeText = new D01AXNormalTimeText(
                scopedContext,
                -18,
                125,
                false,
                states.DepartureMinutes,
                states.DepartureSeconds
            );
            AddChild(departureTimeText);
            // 文字固定
            var departureIndicatorText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.DepartureIndicator.Value)),
                    horizontalAlignment: 0.5f,
                    useHorizontalOverhangMetrics: true
                ),
                contentColor: MonitorColors.White,
                y: 125);
            AddChild(departureIndicatorText);
            var hasDepartureIndicator = CreateComputed(() => !string.IsNullOrEmpty(states.DepartureIndicator.Value));
            departureIndicatorText.IsVisible.Bind(hasDepartureIndicator);
            departureTimeText.IsVisible.Bind(CreateComputed(() => !hasDepartureIndicator.Value));
            var trackNameText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.TrackName.Value)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White,
                x: -18,
                y: 145);
            AddChild(trackNameText);
            SkipArrangementWhenHidden = CreatePropertySlot<bool>(DirtyType.Layout);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public PropertySlot<bool> SkipArrangementWhenHidden { get; }

        public LayoutLength PreferredWidth => LayoutLength.Absolute(0);
        public LayoutLength PreferredHeight => LayoutLength.Absolute(0);
        public float MarginWidth => 82;
        public float MarginHeight => 0;
        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangementWhenHidden;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => true;

        public void SetLayoutSize(float width, float height)
        {
        }
    }

    public class D01AXEDenTimeColStates : ViewModel
    {
        public Signal<bool> IsVisible { get; } = new Signal<bool>();
        public Signal<string> StationName { get; } = new Signal<string>(string.Empty);
        public Signal<string> ArrivalMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> ArrivalSeconds { get; } = new Signal<string>(string.Empty);
        public Signal<string> ArrivalIndicator { get; } = new Signal<string>(string.Empty);
        public Signal<string> StationTask { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureSeconds { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureIndicator { get; } = new Signal<string>(string.Empty);
        public Signal<string> TrackName { get; } = new Signal<string>(string.Empty);
        public Signal<Color4> StationColor { get; } = new Signal<Color4>(MonitorColors.White);
        public Signal<Color4> StationBackgroundColor { get; } = new Signal<Color4>(Colors.Transparent);
    }
}