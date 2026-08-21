using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenLocationIndicator : Widget<D01AXEDenLocationIndicatorStates>
    {
        private readonly Arrow _arrow;
        private RectangleF _selfRelativeDirtyBounds;

        public D01AXEDenLocationIndicator(RenderContext context, D01AXEDenLocationIndicatorStates states)
            : base(context, y: 250)
        {
            ViewModel = states;
            ColumnPixelXCoords = CreateReactiveArray<float>(DirtyType.Visual, D01AXEDenGroupViewModel.MaxColCount);
            _arrow = new Arrow(context);
            AddChild(_arrow);
            IsVisible.Bind(states.IsVisible);
            _arrow.VehicleDirection.Bind(states.VehicleDirection);
            WatchEffect(
                EffectPhase.Visual,
                () =>
                {
                    UpdateArrowPosition();
                    UpdateSelfRelativeDirtyBounds();
                }
            );
            WatchEffect(
                ViewModel.VehicleDirection,
                ViewModel.SegmentCount,
                ViewModel.StationColCount,
                ViewModel.SegmentColors,
                ViewModel.SegmentWidths,
                ViewModel.CurrentStationColumnIndex
            );
        }

        public override RectangleF SelfRelativeDirtyBounds => _selfRelativeDirtyBounds;

        public ReactiveArray<float> ColumnPixelXCoords { get; }

        private void UpdateSelfRelativeDirtyBounds()
        {
            var physicalColCount = ViewModel.StationColCount.Value;
            var colCount = ViewModel.ColCount;
            if (physicalColCount <= 0 || colCount <= 0 || ColumnPixelXCoords.Count < physicalColCount)
            {
                _selfRelativeDirtyBounds = RectangleF.Empty;
                return;
            }

            var isLeft = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Left;
            var startIdx = isLeft ? physicalColCount - colCount : 0;
            var endIdx = isLeft ? physicalColCount - 1 : colCount - 1;

            if (startIdx >= 0 && startIdx < ColumnPixelXCoords.Count && endIdx < ColumnPixelXCoords.Count)
                _selfRelativeDirtyBounds =
                    new RawRectF(ColumnPixelXCoords[startIdx], -10, ColumnPixelXCoords[endIdx], 9);
            else
                _selfRelativeDirtyBounds = RectangleF.Empty;
        }

        private void UpdateArrowPosition()
        {
            var physicalColCount = ViewModel.StationColCount.Value;
            var currentColIndex = ViewModel.CurrentStationColumnIndex.Value;
            var nextColIndex = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Left
                ? currentColIndex - 1
                : currentColIndex + 1;

            if (currentColIndex >= 0 && currentColIndex < physicalColCount)
            {
                if (nextColIndex >= 0 && nextColIndex < physicalColCount)
                {
                    var currentX = ColumnPixelXCoords[currentColIndex];
                    var nextX = ColumnPixelXCoords[nextColIndex];
                    var progress = ViewModel.TrainProgress.Value;
                    var arrowXRaw = MathHelper.Lerp(currentX, nextX, progress);
                    _arrow.X.Value = (int)Math.Round(arrowXRaw, MidpointRounding.AwayFromZero);
                }
                else
                {
                    _arrow.X.Value =
                        (int)Math.Round(ColumnPixelXCoords[currentColIndex], MidpointRounding.AwayFromZero);
                }
            }
            else
            {
                _arrow.X.Value = -1;
            }
        }

        protected override void OnDraw(float totalScale)
        {
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;

            var isLeftDirection = ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Left;
            var colCount = ViewModel.StationColCount.Value;

            for (var i = 0; i < ViewModel.SegmentCount.Value; i++)
            {
                Context.CommonBrush.Color = ViewModel.SegmentColors[i];
                var j = isLeftDirection ? colCount - 1 - i : i;
                var nextIdx = isLeftDirection ? j - 1 : j + 1;

                if (j >= 0 && j < ColumnPixelXCoords.Count &&
                    nextIdx >= 0 && nextIdx < ColumnPixelXCoords.Count)
                {
                    var minX = ColumnPixelXCoords[j];
                    var maxX = ColumnPixelXCoords[nextIdx];
                    if (minX > maxX) (minX, maxX) = (maxX, minX);
                    var minXSnapped = (float)Math.Round(minX, MidpointRounding.AwayFromZero);
                    var maxXSnapped = (float)Math.Round(maxX, MidpointRounding.AwayFromZero);
                    var w = ViewModel.SegmentWidths[i];
                    var y = w % 2 == 0 ? -1f : -0.5f;
                    Context.DeviceContext.DrawLine(
                        new Vector2(minXSnapped, y),
                        new Vector2(maxXSnapped, y),
                        Context.CommonBrush,
                        w
                    );
                }
            }

            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }

        private class Arrow : Widget
        {
            private static readonly Color4 Color = "#9ce8a7".ToColor4();
            private readonly Baker _leftBaker;
            private readonly ID2D1PathGeometry _leftGeometry;
            private readonly Baker _rightBaker;
            private readonly ID2D1PathGeometry _rightGeometry;

            public Arrow(RenderContext context) : base(context)
            {
                VehicleDirection = CreatePropertySlot<TIMSVehicleDirection>(DirtyType.Visual);
                _leftGeometry = CreateLeftGeometry();
                RegisterResource(_leftGeometry);
                _rightGeometry = CreateRightGeometry();
                RegisterResource(_rightGeometry);
                _leftBaker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_leftBaker);
                _rightBaker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_rightBaker);
                WatchEffect(() =>
                {
                    _leftBaker.Refresh();
                    _rightBaker.Refresh();
                }, VehicleDirection);
            }

            public override RectangleF SelfRelativeDirtyBounds => new RawRectF(-9, -8f, 9f, 8f);

            public PropertySlot<TIMSVehicleDirection> VehicleDirection { get; }

            private ID2D1PathGeometry CreateLeftGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(9f, -7.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(9f, 7f));
                    sink.AddLine(new Vector2(0f, 7f));
                    sink.AddLine(new Vector2(-7, -0.5f));
                    sink.AddLine(new Vector2(0f, -7.5f));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            private ID2D1PathGeometry CreateRightGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(-9, -7.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(0, -7.5f));
                    sink.AddLine(new Vector2(7.5f, -0.5f));
                    sink.AddLine(new Vector2(0, 7));
                    sink.AddLine(new Vector2(-9, 7));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            protected override void OnStaticWarmUp(float totalScale)
            {
                _leftBaker.Bake(SelfRelativeDirtyBounds, () => Draw(true));
                _rightBaker.Bake(SelfRelativeDirtyBounds, () => Draw(false));
            }

            protected override void OnDraw(float totalScale)
            {
                if (VehicleDirection.Value == TIMSVehicleDirection.Left)
                    _leftBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(true));
                else
                    _rightBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(false));
            }

            private void Draw(bool isLeft)
            {
                var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                Context.CommonBrush.Color = Color;
                Context.DeviceContext.FillGeometry(isLeft ? _leftGeometry : _rightGeometry, Context.CommonBrush);
                Context.DeviceContext.AntialiasMode = oldAntialiasMode;
            }
        }
    }

    public class D01AXEDenLocationIndicatorStates : ViewModel
    {
        public D01AXEDenLocationIndicatorStates()
        {
            SegmentColors = CreateReactiveArray<Color4>(D01AXEDenGroupViewModel.MaxColCount);
            SegmentWidths = CreateReactiveArray<int>(D01AXEDenGroupViewModel.MaxColCount);
        }

        public Signal<bool> IsVisible { get; } = new Signal<bool>();
        public Signal<TIMSVehicleDirection> VehicleDirection { get; } = new Signal<TIMSVehicleDirection>();
        public Signal<int> SegmentCount { get; } = new Signal<int>();
        public int ColCount => SegmentCount.Value + 1;
        public Signal<int> StationColCount { get; } = new Signal<int>();
        public ReactiveArray<Color4> SegmentColors { get; }
        public ReactiveArray<int> SegmentWidths { get; }
        public Signal<float> TrainProgress { get; } = new Signal<float>();
        public Signal<int> CurrentStationColumnIndex { get; } = new Signal<int>(-1);
    }
}