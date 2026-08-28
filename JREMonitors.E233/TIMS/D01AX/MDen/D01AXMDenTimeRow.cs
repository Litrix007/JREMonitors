using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.MDen
{
    public class D01AXMDenTimeRow : Widget<D01AXMDenTimeRowStates>
    {
        public D01AXMDenTimeRow(RenderContext context, ScopedRenderContext scopedContext, int y,
            D01AXMDenTimeRowStates states) : base(context, 0, y)
        {
            ViewModel = states;
            IsVisible.Bind(states.IsVisible);
            var durationText = new D01AXNormalTimeText(
                scopedContext, 15, 18, 0, false,
                ViewModel.DurationMinutes, ViewModel.DurationSeconds
            );
            durationText.Color.Bind(ViewModel.DurationColor);
            AddChild(durationText);
            var stationText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.StationName.Value)),
                    2,
                    context: scopedContext
                ),
                contentColor: MonitorColors.White, x: 90);
            stationText.ContentColor.Bind(ViewModel.StationColor);
            AddChild(stationText);
            var arrivalTimeText = new D01AXMDenTimeText(
                context, scopedContext, 255, 0, 73,
                ViewModel.ArrivalHoursAndMinutes,
                ViewModel.ArrivalSeconds,
                ViewModel.ArrivalIndicator,
                ViewModel.ShowArrivalPassArrow,
                ViewModel.ArrivalColor,
                ViewModel.ArrivalColor
            );
            AddChild(arrivalTimeText);
            var departureTimeText = new D01AXMDenTimeText(
                context, scopedContext, 462, 0, 73,
                ViewModel.DepartureHoursAndMinutes,
                ViewModel.DepartureSeconds,
                ViewModel.DepartureIndicator,
                ViewModel.ShowDeparturePassArrow,
                ViewModel.DepartureColor,
                ViewModel.DepartureColor
            );
            AddChild(departureTimeText);
            var trackNameText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.TrackName.Value)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White, x: 670);
            trackNameText.ContentColor.Bind(ViewModel.TrackColor);
            AddChild(trackNameText);
            var speedLimitArrivalText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.SpeedLimitArrival.Value)),
                    horizontalAlignment: 1,
                    context: scopedContext
                ),
                contentColor: Colors.Yellow, x: 790);
            AddChild(speedLimitArrivalText);
            var speedLimitDepartureText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.SpeedLimitDeparture.Value)),
                    horizontalAlignment: 1,
                    context: scopedContext
                ),
                contentColor: Colors.Yellow, x: 790, y: 18);
            AddChild(speedLimitDepartureText);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D01AXMDenTimeRowStates : ViewModel
    {
        public D01AXMDenTimeRowStates()
        {
            IsVisible = CreatePropertySlot(false);
            DurationMinutes = CreatePropertySlot(new string('\u3000', 2));
            DurationSeconds = CreatePropertySlot(new string('\u3000', 2));
            StationName = CreatePropertySlot(new string('\u3000', 4));
            ArrivalHoursAndMinutes = CreatePropertySlot("  :  ".ToFullWidth());
            ArrivalSeconds = CreatePropertySlot(new string('\u3000', 2));
            ArrivalIndicator = CreatePropertySlot("\u3000");
            ShowArrivalPassArrow = CreatePropertySlot(false);
            ShowDeparturePassArrow = CreatePropertySlot(false);
            ArrivalColor = CreatePropertySlot(MonitorColors.White);
            DepartureHoursAndMinutes = CreatePropertySlot("  :  ".ToFullWidth());
            DepartureSeconds = CreatePropertySlot(new string('\u3000', 2));
            DepartureIndicator = CreatePropertySlot("\u3000");
            DepartureColor = CreatePropertySlot(MonitorColors.White);
            TrackName = CreatePropertySlot(new string('\u3000', 2));
            SpeedLimitArrival = CreatePropertySlot(new string('\u3000', 2));
            SpeedLimitDeparture = CreatePropertySlot(new string('\u3000', 3));
            DurationColor = CreatePropertySlot(MonitorColors.White);
            StationColor = CreatePropertySlot(MonitorColors.White);
            TrackColor = CreatePropertySlot(MonitorColors.White);
        }

        public PropertySlot<bool> IsVisible { get; }
        public PropertySlot<string> DurationMinutes { get; }
        public PropertySlot<string> DurationSeconds { get; }
        public PropertySlot<string> StationName { get; }
        public PropertySlot<string> ArrivalHoursAndMinutes { get; }
        public PropertySlot<string> ArrivalSeconds { get; }
        public PropertySlot<string> ArrivalIndicator { get; }
        public PropertySlot<bool> ShowArrivalPassArrow { get; }
        public PropertySlot<bool> ShowDeparturePassArrow { get; }
        public PropertySlot<Color4> ArrivalColor { get; }
        public PropertySlot<string> DepartureHoursAndMinutes { get; }
        public PropertySlot<string> DepartureSeconds { get; }
        public PropertySlot<string> DepartureIndicator { get; }
        public PropertySlot<Color4> DepartureColor { get; }
        public PropertySlot<string> TrackName { get; }
        public PropertySlot<string> SpeedLimitArrival { get; }
        public PropertySlot<string> SpeedLimitDeparture { get; }
        public PropertySlot<Color4> DurationColor { get; }
        public PropertySlot<Color4> StationColor { get; }
        public PropertySlot<Color4> TrackColor { get; }
    }
}