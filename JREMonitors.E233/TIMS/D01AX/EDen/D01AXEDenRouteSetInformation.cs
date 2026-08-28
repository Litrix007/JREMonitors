using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.E233.ViewModels;
using JREMonitors.JRE;
using JREMonitors.JRE.Providers;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenRouteSetInformation : Widget<D01AXEDenRouteSetInformationViewModel>
    {
        private const float InfoX = 60;
        private const float InfoSectionTop = 3;
        private const float InfoSectionFullWidth = 32;
        private const float InfoSectionUnitHeight = 17;
        private const int StrokeWidth = 2;
        private const int UpperHalfStrokeWidth = 1 + StrokeWidth / 2;
        private readonly BoundsDrawerWidget _atcHoldingBrakeActiveText;
        private readonly List<int> _sectionCoordsBuffer = new List<int>(10);
        private readonly List<StationBlockCoords> _stationBlockCoordsBuffer = new List<StationBlockCoords>(5);

        public D01AXEDenRouteSetInformation(RenderContext context) : base(context, y: 132)
        {
            ViewModel = new D01AXEDenRouteSetInformationViewModel();
            IsVisible.Bind(ViewModel.IsVisible);

            var info = new Info(context);
            AddChild(info);
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("1000", horizontalAlignment: 1),
                contentColor: Colors.Yellow, x: InfoX - 20, y: InfoSectionTop));
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("500", horizontalAlignment: 1),
                contentColor: Colors.Yellow, x: InfoX - 20, y: InfoSectionTop + InfoSectionUnitHeight * 5));
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("0", horizontalAlignment: 1),
                contentColor: Colors.Yellow, x: InfoX - 20, y: InfoSectionTop + InfoSectionUnitHeight * 10));
            var stopSectionNameText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(ViewModel.StopSectionName)),
                    cache: false),
                contentColor: Colors.Yellow, x: InfoX + 20, y: InfoSectionTop);
            AddChild(stopSectionNameText);
            var currentSectionNameText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(ViewModel.CurrentSectionName)),
                    cache: false),
                contentColor: Colors.Yellow, x: InfoX + 20, y: InfoSectionTop + InfoSectionUnitHeight * 10);
            AddChild(currentSectionNameText);
            _atcHoldingBrakeActiveText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("転動防止動作", horizontalAlignment: 0.5f,
                    arrangement: ContentArrangement.Center),
                targetBounds: new RectangleF(0, 0, 113, 20),
                x: (float)Math.Round(InfoX - 113 / 2f, MidpointRounding.AwayFromZero), y: 200,
                contentColor: Colors.Black,
                backgroundColor: Colors.Yellow);
            _atcHoldingBrakeActiveText.IsVisible.Bind(ViewModel.IsAtcHoldingBrakeActive);
            AddChild(_atcHoldingBrakeActiveText);
            WatchEffect(EffectPhase.State, () =>
            {
                if (!ViewModel.IsVisible) return;
                var vehicleLocation = ViewModel.VehicleLocation.Value;
                const float pixelsPerMeter = InfoSectionUnitHeight / 100f;
                var baseYInt = (int)Math.Round(InfoSectionUnitHeight * 10f, MidpointRounding.AwayFromZero);
                var cameraWorldY = GetWorldPixelY(vehicleLocation, pixelsPerMeter);
                _stationBlockCoordsBuffer.Clear();
                foreach (var range in ViewModel.StationBlockRanges)
                {
                    var startWorldY = GetWorldPixelY(range.StartLocation, pixelsPerMeter);
                    var bottomY = baseYInt + (startWorldY - cameraWorldY);
                    var physicalLength = (float)(range.EndLocation - range.StartLocation);
                    var heightPixels = SnapPixel(physicalLength * pixelsPerMeter);
                    var topY = bottomY - heightPixels;
                    _stationBlockCoordsBuffer.Add(new StationBlockCoords(topY, bottomY));
                }

                info.StationBlockCoordsList.Update(_stationBlockCoordsBuffer);
                _sectionCoordsBuffer.Clear();
                foreach (var sec in ViewModel.Sections)
                {
                    var secWorldY = GetWorldPixelY(sec.Location, pixelsPerMeter);
                    var y = baseYInt + (secWorldY - cameraWorldY);
                    _sectionCoordsBuffer.Add(y);
                }

                info.SectionCoords.Update(_sectionCoordsBuffer);
                if (ViewModel.StopSection.Value.HasValue)
                {
                    var stopSec = ViewModel.StopSection.Value.Value;
                    var stopWorldY = GetWorldPixelY(stopSec.Location, pixelsPerMeter);
                    var sectionYInt = baseYInt + (stopWorldY - cameraWorldY);
                    info.StopCoord.Value = sectionYInt - 16 - UpperHalfStrokeWidth;
                }
                else
                {
                    info.StopCoord.Value = null;
                }
            });
        }

        protected override bool SkipUpdateWhenHidden => false;

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, 134, 228);
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        private static int SnapPixel(float value)
        {
            return (int)Math.Floor(value + 0.5f);
        }

        private static int GetWorldPixelY(double location, float pixelsPerMeter)
        {
            return SnapPixel((float)(-location * pixelsPerMeter));
        }


        protected override void OnDraw(float totalScale)
        {
            Context.DeviceContext.WithAliasedIfNeeded(() =>
            {
                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
            });
        }

        public struct StationBlockCoords : IEquatable<StationBlockCoords>
        {
            public readonly int Top;
            public readonly int Bottom;

            public StationBlockCoords(int top, int bottom)
            {
                Top = top;
                Bottom = bottom;
            }

            public bool Equals(StationBlockCoords other)
            {
                return Top == other.Top && Bottom == other.Bottom;
            }

            public override bool Equals(object obj)
            {
                return obj is StationBlockCoords other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Top, Bottom);
            }
        }

        public class Info : Widget
        {
            private readonly Baker _baker;
            private readonly ID2D1PathGeometry _selfGeometry;
            private readonly ID2D1PathGeometry _stopGeometry;

            public Info(RenderContext context) : base(context, InfoX, InfoSectionTop)
            {
                SectionCoords = CreateReactiveList<int>(DirtyType.Visual);
                StationBlockCoordsList = CreateReactiveList<StationBlockCoords>(DirtyType.Visual);
                StopCoord = CreatePropertySlot<int?>(DirtyType.Visual);
                _selfGeometry = CreateSelfGeometry();
                RegisterResource(_selfGeometry);
                _stopGeometry = CreateStopGeometry();
                RegisterResource(_stopGeometry);
                _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_baker);
                WatchEffect(() => { _baker.Refresh(); }, SectionCoords, StationBlockCoordsList, StopCoord);
            }

            public override RectangleF SelfRelativeDirtyBounds =>
                new RectangleF(-16, 0, InfoSectionFullWidth, InfoSectionUnitHeight * 11);

            public ReactiveList<int> SectionCoords { get; }
            public PropertySlot<int?> StopCoord { get; }
            public ReactiveList<StationBlockCoords> StationBlockCoordsList { get; }

            private ID2D1PathGeometry CreateSelfGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(0, 0), FigureBegin.Filled);
                    sink.AddLine(new Vector2(InfoSectionFullWidth / 2, InfoSectionUnitHeight / 2 - 0.5f));
                    sink.AddLine(new Vector2(InfoSectionFullWidth / 2, InfoSectionUnitHeight));
                    sink.AddLine(new Vector2(-InfoSectionFullWidth / 2, InfoSectionUnitHeight));
                    sink.AddLine(new Vector2(-InfoSectionFullWidth / 2, InfoSectionUnitHeight / 2 - 0.5f));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            private ID2D1PathGeometry CreateStopGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(0, -1), FigureBegin.Filled);
                    sink.AddLine(new Vector2(InfoSectionFullWidth / 2, InfoSectionUnitHeight - 1));
                    sink.AddLine(new Vector2(-InfoSectionFullWidth / 2, InfoSectionUnitHeight - 1));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            protected override void OnDraw(float totalScale)
            {
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, () =>
                {
                    var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                    Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                    Context.CommonBrush.Color = "#3E32E6".ToColor4();
                    const float areaBottom = InfoSectionUnitHeight * 10;
                    var stopCoord = StopCoord.Value;
                    var stopCoordBottom = stopCoord + 16 + UpperHalfStrokeWidth;

                    if (stopCoordBottom.HasValue)
                    {
                        if (stopCoordBottom.Value <= areaBottom)
                            Context.DeviceContext.FillRectangle(
                                new RawRectF(-InfoSectionFullWidth / 4, stopCoordBottom.Value, InfoSectionFullWidth / 4,
                                    areaBottom),
                                Context.CommonBrush);
                    }
                    else
                    {
                        Context.DeviceContext.FillRectangle(
                            new RawRectF(-InfoSectionFullWidth / 4, 0,
                                InfoSectionFullWidth / 4,
                                InfoSectionUnitHeight * 10), Context.CommonBrush);
                    }

                    for (var i = 0; i < StationBlockCoordsList.Count; i++)
                    {
                        var coords = StationBlockCoordsList[i];
                        var top = MathHelper.Max(coords.Top, 0);
                        if (stopCoordBottom > top) top = stopCoordBottom.Value;

                        var bottom = MathHelper.Min(coords.Bottom, areaBottom);
                        if (top > bottom) continue;
                        Context.DeviceContext.FillRectangle(
                            new RawRectF(-InfoSectionFullWidth / 2, top,
                                InfoSectionFullWidth / 2,
                                bottom), Context.CommonBrush);
                    }

                    Context.CommonBrush.Color = Colors.Black;
                    for (var i = 0; i < SectionCoords.Count; i++)
                    {
                        var y = SectionCoords[i];
                        const float offset = StrokeWidth % 2 == 0 ? -1f : -0.5f;
                        Context.DeviceContext.DrawLine(
                            new Vector2(-InfoSectionFullWidth / 2, y + offset),
                            new Vector2(InfoSectionFullWidth / 2, y + offset),
                            Context.CommonBrush,
                            StrokeWidth
                        );
                    }

                    var oldTransform = Context.DeviceContext.Transform;
                    if (stopCoord.HasValue)
                    {
                        Context.CommonBrush.Color = MonitorColors.White;
                        Context.DeviceContext.FillRectangle(
                            new RawRectF(-InfoSectionFullWidth / 4, 0,
                                InfoSectionFullWidth / 4,
                                stopCoord.Value - StrokeWidth), Context.CommonBrush);
                        Context.CommonBrush.Color = "#DF473D".ToColor4();
                        Context.DeviceContext.Transform =
                            Matrix3x2.CreateTranslation(0, stopCoord.Value) * oldTransform;
                        Context.DeviceContext.FillGeometry(_stopGeometry, Context.CommonBrush);
                    }

                    Context.CommonBrush.Color = MonitorColors.White;
                    Context.DeviceContext.Transform =
                        Matrix3x2.CreateTranslation(0, InfoSectionUnitHeight * 10) * oldTransform;
                    Context.DeviceContext.FillGeometry(_selfGeometry, Context.CommonBrush);
                    Context.DeviceContext.Transform = oldTransform;
                    Context.DeviceContext.AntialiasMode = oldAntialiasMode;
                });
            }
        }
    }

    public class D01AXEDenRouteSetInformationViewModel : ViewModel
    {
        private readonly List<TIMSSignalSection> _sectionBuffer = new List<TIMSSignalSection>(10);
        private readonly List<TIMSStationBlockRange> _stationBlockRangeBuffer = new List<TIMSStationBlockRange>(5);
        private TIMSICCardService<E233SignalSystem> _icCardService;
        private ISignalProvider<E233SignalSystem> _signalProvider;
        private IVehicleStateProvider _vehicleStateProvider;

        public D01AXEDenRouteSetInformationViewModel()
        {
            IsVisible = new Signal<bool>();
            VehicleLocation = new Signal<float>();
            CurrentSectionName = new Signal<string>();
            StopSection = new Signal<TIMSSignalSection?>();
            StopSectionName = new Signal<string>();
            Sections = CreateReactiveList<TIMSSignalSection>();
            StationBlockRanges = CreateReactiveList<TIMSStationBlockRange>();
            IsAtcHoldingBrakeActive = new Signal<bool>();
            var normalAtcViewModel = new NormalAtcViewModel();
            IsAtcHoldingBrakeActive = normalAtcViewModel.IsAtcHoldingBrakeActive;
            AddSubViewModel(normalAtcViewModel);
        }

        public Signal<bool> IsVisible { get; }
        public Signal<float> VehicleLocation { get; }
        public Signal<string> CurrentSectionName { get; }
        public Signal<TIMSSignalSection?> StopSection { get; }
        public Signal<string> StopSectionName { get; }
        public ReactiveList<TIMSSignalSection> Sections { get; }
        public ReactiveList<TIMSStationBlockRange> StationBlockRanges { get; }
        public Signal<bool> IsAtcHoldingBrakeActive { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            _vehicleStateProvider = dataHub.Get<IVehicleStateProvider>();
            _signalProvider = dataHub.Get<ISignalProvider<E233SignalSystem>>();
            _icCardService = dataHub.Get<TIMSICCardService<E233SignalSystem>>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            IsVisible.Value = _signalProvider.ActiveSignalSystem == E233SignalSystem.Datc;
            var vehicleLocation = _vehicleStateProvider.Location;
            VehicleLocation.Value = (float)vehicleLocation;
            var stopSectionIndex = _signalProvider.StopSectionIndex;
            var globalSections = _signalProvider.SignalSections;
            if (stopSectionIndex >= 0 && stopSectionIndex < globalSections.Count)
            {
                StopSection.Value = globalSections[stopSectionIndex];
                StopSectionName.Value = StopSection.Value.Value.Location < vehicleLocation + 1000
                    ? globalSections[stopSectionIndex].Name
                    : string.Empty;
            }
            else
            {
                StopSection.Value = null;
                StopSectionName.Value = string.Empty;
            }

            _sectionBuffer.Clear();
            var currentSectionIndex = _signalProvider.CurrentSectionIndex;
            CurrentSectionName.Value = currentSectionIndex >= 0 && currentSectionIndex < globalSections.Count
                ? globalSections[currentSectionIndex].Name
                : string.Empty;

            var startSearchIdx = currentSectionIndex >= 0 ? currentSectionIndex : 0;
            for (var i = startSearchIdx; i < globalSections.Count; i++)
            {
                var sec = globalSections[i];
                if (sec.Location >= vehicleLocation + 1000 ||
                    (StopSection.Value.HasValue && sec.Location > StopSection.Value.Value.Location))
                    break;

                if (sec.Location >= vehicleLocation - 100) _sectionBuffer.Add(sec);
            }

            Sections.Update(_sectionBuffer);

            _stationBlockRangeBuffer.Clear();
            if (_icCardService.Legs != null && _icCardService.Legs.Count > 0)
            {
                var startLegIdx = -1;
                var startStnIdx = -1;
                var currentStnIdxIgnore = _icCardService.CurrentStationIndexIgnoreInserted;
                var currentLegIdxIgnore = _icCardService.CurrentLegIndexIgnoreInserted;
                if (currentStnIdxIgnore >= 0 && currentLegIdxIgnore >= 0 &&
                    currentLegIdxIgnore < _icCardService.Legs.Count)
                {
                    var currentLegStations = _icCardService.Legs[currentLegIdxIgnore].Stations;
                    if (currentLegStations != null && currentStnIdxIgnore < currentLegStations.Count)
                    {
                        startLegIdx = currentLegIdxIgnore;
                        startStnIdx = currentStnIdxIgnore;
                    }
                }

                if (startLegIdx == -1)
                {
                    startLegIdx = 0;
                    startStnIdx = 0;
                }

                var breakAll = false;
                for (var l = startLegIdx; l < _icCardService.Legs.Count; l++)
                {
                    var leg = _icCardService.Legs[l];
                    if (leg?.Stations == null || leg.Stations.Count == 0) continue;

                    var stnStart = 0;
                    if (l == startLegIdx)
                    {
                        stnStart = startStnIdx;
                    }
                    else
                    {
                        if (_icCardService.IsOverlapStation(l, 0)) stnStart = 1;
                    }

                    for (var i = stnStart; i < leg.Stations.Count; i++)
                    {
                        var station = leg.Stations[i];
                        if (station == null) continue;

                        var start = station.Location - station.StationBlockStartOffset;
                        var end = station.Location + station.StationBlockEndOffset;

                        if (start >= vehicleLocation + 1000 ||
                            (StopSection.Value.HasValue && start > StopSection.Value.Value.Location))
                        {
                            breakAll = true;
                            break;
                        }

                        if (end < vehicleLocation - 100) continue;

                        _stationBlockRangeBuffer.Add(new TIMSStationBlockRange(start, end));
                    }

                    if (breakAll) break;
                }
            }

            StationBlockRanges.Update(_stationBlockRangeBuffer);
        }
    }
}