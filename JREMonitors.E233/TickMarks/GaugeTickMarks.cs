using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TickMarks
{
    public class GaugeTickMarks : Widget
    {
        private readonly Baker _baker;
        private readonly PropertySlot<int> _majorScaleCount;
        private readonly PropertySlot<Action<GaugeTickMarkState>> _majorTickMarkAction;
        private readonly PropertySlot<Color4> _majorTickMarkColor;
        private readonly PropertySlot<float> _majorTickMarkStrokeWidth;
        private readonly PropertySlot<float> _majorTickMarkWidth;
        private readonly PropertySlot<Color4> _minorBoldTickMarkColor;
        private readonly PropertySlot<int> _minorScaleCount;
        private readonly PropertySlot<float> _minorTickMarkBoldedWidth;
        private readonly PropertySlot<int> _minorTickMarkBoldInterval;
        private readonly PropertySlot<float> _minorTickMarkBoldStrokeWidth;
        private readonly PropertySlot<Color4> _minorTickMarkColor;
        private readonly PropertySlot<float> _minorTickMarkOffset;
        private readonly PropertySlot<float> _minorTickMarkStrokeWidth;
        private readonly PropertySlot<float> _minorTickMarkWidth;
        private readonly PropertySlot<float> _radius;
        private readonly PropertySlot<float> _startAngleInDegrees;
        private readonly ID2D1StrokeStyle1 _strokeStyle;
        private readonly PropertySlot<float> _sweepAngleInDegrees;

        public GaugeTickMarks(RenderContext context, float x, float y) : base(context, x, y)
        {
            _strokeStyle = context.D2D1Factory.CreateStrokeStyle(GeometryHelper.RoundStrokeStyleProperties);
            RegisterResource(_strokeStyle);
            _baker = new Baker(context);
            RegisterResource(_baker);
            _majorTickMarkColor = CreatePropertySlot(DirtyType.Visual, MonitorColors.White);
            _minorTickMarkColor = CreatePropertySlot(DirtyType.Visual, MonitorColors.MinorTickMark);
            _minorBoldTickMarkColor = CreatePropertySlot(DirtyType.Visual, MonitorColors.White);
            _radius = CreatePropertySlot<float>(DirtyType.Visual);
            _startAngleInDegrees = CreatePropertySlot<float>(DirtyType.Visual);
            _sweepAngleInDegrees = CreatePropertySlot<float>(DirtyType.Visual);
            _majorScaleCount = CreatePropertySlot<int>(DirtyType.Visual);
            _minorScaleCount = CreatePropertySlot<int>(DirtyType.Visual);
            _majorTickMarkWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkBoldedWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _majorTickMarkStrokeWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkStrokeWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkBoldStrokeWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkOffset = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkBoldInterval = CreatePropertySlot<int>(DirtyType.Visual);
            _majorTickMarkAction = CreatePropertySlot<Action<GaugeTickMarkState>>(DirtyType.Visual);
            WatchEffect(
                () => { _baker.Refresh(); },
                _majorTickMarkColor,
                _minorTickMarkColor,
                _minorBoldTickMarkColor,
                _radius,
                _startAngleInDegrees,
                _sweepAngleInDegrees,
                _majorScaleCount,
                _minorScaleCount,
                _majorTickMarkWidth,
                _minorTickMarkWidth,
                _minorTickMarkBoldedWidth,
                _majorTickMarkStrokeWidth,
                _minorTickMarkStrokeWidth,
                _minorTickMarkBoldStrokeWidth,
                _minorTickMarkOffset,
                _minorTickMarkBoldInterval,
                _majorTickMarkAction
            );
        }

        private RectangleF BakeBounds
        {
            get
            {
                var bounds = GeometryHelper.GetArcBounds(Radius - 5, Radius + MajorTickMarkWidth + 5,
                    StartAngleInDegrees,
                    SweepAngleInDegrees);
                var spread = Shadows.TickMarkDrop.Max(shadow => shadow.MaxSpread);
                RectangleF boundsInflate = BoundsInflate;
                bounds = new RawRectF(bounds.Left + boundsInflate.Left - spread,
                    bounds.Top + boundsInflate.Top - spread,
                    bounds.Right + boundsInflate.Right + spread, bounds.Bottom + boundsInflate.Bottom + spread);
                return bounds.SnapToPixels();
            }
        }

        public override RectangleF SelfRelativeDirtyBounds => BakeBounds;

        public RawRectF BoundsInflate { get; set; }

        public Color4 MajorTickMarkColor
        {
            get => _majorTickMarkColor;
            set => _majorTickMarkColor.Value = value;
        }

        public Color4 MinorTickMarkColor
        {
            get => _minorTickMarkColor;
            set => _minorTickMarkColor.Value = value;
        }

        public Color4 MinorBoldTickMarkColor
        {
            get => _minorBoldTickMarkColor;
            set => _minorBoldTickMarkColor.Value = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius.Value = value;
        }

        public float StartAngleInDegrees
        {
            get => _startAngleInDegrees;
            set => _startAngleInDegrees.Value = value;
        }

        public float SweepAngleInDegrees
        {
            get => _sweepAngleInDegrees;
            set => _sweepAngleInDegrees.Value = value;
        }

        public int MajorScaleCount
        {
            get => _majorScaleCount;
            set => _majorScaleCount.Value = value;
        }

        public int MinorScaleCount
        {
            get => _minorScaleCount;
            set => _minorScaleCount.Value = value;
        }

        public float MajorTickMarkWidth
        {
            get => _majorTickMarkWidth;
            set => _majorTickMarkWidth.Value = value;
        }

        public float MinorTickMarkWidth
        {
            get => _minorTickMarkWidth;
            set => _minorTickMarkWidth.Value = value;
        }

        public float MinorTickMarkBoldedWidth
        {
            get => _minorTickMarkBoldedWidth;
            set => _minorTickMarkBoldedWidth.Value = value;
        }

        public float MajorTickMarkStrokeWidth
        {
            get => _majorTickMarkStrokeWidth;
            set => _majorTickMarkStrokeWidth.Value = value;
        }

        public float MinorTickMarkStrokeWidth
        {
            get => _minorTickMarkStrokeWidth;
            set => _minorTickMarkStrokeWidth.Value = value;
        }

        public float MinorTickMarkBoldStrokeWidth
        {
            get => _minorTickMarkBoldStrokeWidth;
            set => _minorTickMarkBoldStrokeWidth.Value = value;
        }

        public float MinorTickMarkOffset
        {
            get => _minorTickMarkOffset;
            set => _minorTickMarkOffset.Value = value;
        }

        public int MinorTickMarkBoldInterval
        {
            get => _minorTickMarkBoldInterval;
            set => _minorTickMarkBoldInterval.Value = value;
        }

        public Action<GaugeTickMarkState> MajorTickMarkAction
        {
            get => _majorTickMarkAction;
            set => _majorTickMarkAction.Value = value;
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(BakeBounds, Draw);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(BakeBounds, Draw);
        }

        private void Draw()
        {
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop, () =>
            {
                for (var i = 0; i <= MajorScaleCount; i++)
                {
                    var scaleMajor = SweepAngleInDegrees / MajorScaleCount;
                    var angleMajor = StartAngleInDegrees + SweepAngleInDegrees * i / MajorScaleCount;
                    var fromLengthMajor = Radius + MajorTickMarkWidth;
                    var fromMajor = Pos.ForwardByAngle(angleMajor, fromLengthMajor);
                    var toMajor = Pos.ForwardByAngle(angleMajor, Radius);
                    Context.CommonBrush.Color = MajorTickMarkColor;
                    Context.DeviceContext.DrawLine(fromMajor, toMajor, Context.CommonBrush, MajorTickMarkStrokeWidth,
                        _strokeStyle);
                    MajorTickMarkAction?.Invoke(new GaugeTickMarkState
                    {
                        Index = i,
                        Angle = angleMajor,
                        Outer = fromMajor
                    });
                    if (i == MajorScaleCount) continue;
                    for (var j = 1; j < MinorScaleCount; j++)
                    {
                        var angleMinor = angleMajor + scaleMajor * j / MinorScaleCount;
                        var fromLengthMinor = fromLengthMajor - MinorTickMarkOffset;
                        var fromMinor = Pos.ForwardByAngle(angleMinor, fromLengthMinor);
                        if (MinorTickMarkBoldInterval > 0 && j % MinorTickMarkBoldInterval == 0)
                        {
                            Context.CommonBrush.Color = MinorBoldTickMarkColor;
                            Context.DeviceContext.DrawLine(fromMinor,
                                Pos.ForwardByAngle(angleMinor,
                                    fromLengthMinor - MinorTickMarkBoldedWidth),
                                Context.CommonBrush, MinorTickMarkBoldStrokeWidth, _strokeStyle);
                        }
                        else
                        {
                            Context.CommonBrush.Color = MinorTickMarkColor;
                            Context.DeviceContext.DrawLine(fromMinor,
                                Pos.ForwardByAngle(angleMinor, fromLengthMinor - MinorTickMarkWidth),
                                Context.CommonBrush, MinorTickMarkStrokeWidth, _strokeStyle);
                        }
                    }
                }
            });
        }

        public struct GaugeTickMarkState
        {
            public int Index { get; set; }
            public float Angle { get; set; }
            public Vector2 Outer { get; set; }
        }
    }
}