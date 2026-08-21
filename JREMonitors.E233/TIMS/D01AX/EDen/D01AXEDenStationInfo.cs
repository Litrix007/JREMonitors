using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenStationInfo : Widget<D01AXEDenStationInfoStates>
    {
        private readonly D01AXNormalTimeText _departureText;
        private readonly BoundsDrawerWidget _stationNameText;
        private readonly BoundsDrawerWidget _trackNameText;

        public D01AXEDenStationInfo(RenderContext context, ScopedRenderContext scopedContext,
            D01AXEDenStationInfoStates states)
            : base(context, y: 136)
        {
            ViewModel = states;
            IsVisible.Bind(states.IsVisible);
            AddChild(new D01AXNormalTimeText(
                scopedContext,
                0,
                85,
                1,
                false,
                states.TopHoursAndMinutes,
                states.TopSeconds
            ));
            _trackNameText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.TrackName.Value)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White,
                y: 105);
            AddChild(_trackNameText);
            _departureText = new D01AXNormalTimeText(
                scopedContext,
                0,
                125,
                1,
                false,
                states.BottomHoursAndMinutes,
                states.BottomSeconds,
                states.DepartureChar
            );
            _departureText.IsVisible.Bind(CreateComputed(() => !states.IsTimingStation.Value));
            AddChild(_departureText);
            _stationNameText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(states.StationName.Value)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White,
                y: 125);
            _stationNameText.IsVisible.Bind(states.IsTimingStation);
            _stationNameText.ContentColor.Bind(states.StationColor);
            _stationNameText.BackgroundColor.Bind(states.StationBackgroundColor);
            AddChild(_stationNameText);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D01AXEDenStationInfoStates : ViewModel
    {
        public Signal<bool> IsVisible { get; } = new Signal<bool>(true);
        public Signal<string> TopHoursAndMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> TopSeconds { get; } = new Signal<string>(string.Empty);
        public Signal<string> BottomHoursAndMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> BottomSeconds { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureChar { get; } = new Signal<string>(string.Empty);
        public Signal<string> TrackName { get; } = new Signal<string>(string.Empty);
        public Signal<string> StationName { get; } = new Signal<string>(string.Empty);
        public Signal<Color4> StationColor { get; } = new Signal<Color4>();
        public Signal<Color4> StationBackgroundColor { get; } = new Signal<Color4>();
        public Signal<bool> IsTimingStation { get; } = new Signal<bool>();
    }
}