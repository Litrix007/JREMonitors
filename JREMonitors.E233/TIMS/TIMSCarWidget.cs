using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCarWidget : Widget, ILayoutable
    {
        public const float Height = 38;
        private static readonly PropertyKey GeometryCacheKey = new PropertyKey($"{nameof(TIMSCarWidget)}GeometryCache");
        private readonly BoundsDrawerWidget _carNumberText;
        private readonly TIMSVehicleSpec _spec;
        private readonly PropertySlot<float> _width;

        public TIMSCarWidget(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            _spec = spec;
            PreferredWidth = CreatePropertySlot(DirtyType.Layout, LayoutLength.Absolute(0));
            CarType = CreatePropertySlot(DirtyType.Visual, TIMSCarType.TrailerCar);
            PantoGraphType = CreatePropertySlot(DirtyType.Visual, TIMSCarPantoGraphType.None);
            BackgroundColor = CreatePropertySlot<Color4?>(DirtyType.Visual);
            StrokeColor = CreatePropertySlot<Color4>(DirtyType.Visual);
            _width = CreatePropertySlot<float>(DirtyType.Visual);
            CarNumber = CreateRelayPropertySlot<int>();
            _carNumberText = new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer(CreateComputed(() =>
            {
                var text = CarNumber.Value.ToString();
                return RichTextParser.Raw(CarNumber.Value > 9 ? text : text.ToFullWidth());
            }), horizontalAlignment: 0.5f, verticalAlignment: 0.5f));
            _carNumberText.TargetBounds.Bind(CreateComputed(() => new RectangleF(0, 10, _width, 20)));
            _carNumberText.ContentColor.Bind(CreateComputed(() => BackgroundColor.Value.HasValue
                ? MonitorColors.TIMSScreenBackground
                : MonitorColors.TIMSTitleGrey));
            AddChild(_carNumberText);
        }

        public PropertySlot<LayoutLength> PreferredWidth { get; }
        public PropertySlot<TIMSCarType> CarType { get; }
        public PropertySlot<TIMSCarPantoGraphType> PantoGraphType { get; }
        public PropertySlot<Color4?> BackgroundColor { get; }
        public PropertySlot<Color4> StrokeColor { get; }
        public PropertySlot<int> CarNumber { get; }

        public sealed override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, _width.Value, Height);
        LayoutLength ILayoutable.PreferredWidth => PreferredWidth.Value;
        LayoutLength ILayoutable.PreferredHeight => LayoutLength.Absolute(Height);
        float ILayoutable.MarginWidth => 0;
        float ILayoutable.MarginHeight => 0;
        public bool SkipArrangeWhenHidden { get; set; } = true;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible { get; set; } = true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden { get; set; }

        public void SetLayoutSize(float width, float height)
        {
            _width.Value = width;
        }

        private ID2D1PathGeometry GetOrCreateGeometry(CarGeometryKey key)
        {
            var cache = Context.GetResourceCache<CarGeometryKey, ID2D1PathGeometry>(GeometryCacheKey);
            return cache.GetOrCreate(key, (widget: this, key), s => s.widget.BuildCarGeometry(s.key));
        }

        private ID2D1PathGeometry BuildCarGeometry(CarGeometryKey key)
        {
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                switch (key.CarType)
                {
                    case TIMSCarType.FirstCar:
                        if (key.IsCarReverseArrangement) AddRightCarFigure(sink, key.Width);
                        else AddLeftCarFigure(sink, key.Width);
                        break;
                    case TIMSCarType.LastCar:
                        if (key.IsCarReverseArrangement) AddLeftCarFigure(sink, key.Width);
                        else AddRightCarFigure(sink, key.Width);
                        break;
                    case TIMSCarType.GreenCar:
                        AddGreenCarFigure(sink, key.Width);
                        break;
                    case TIMSCarType.MotorCar:
                    case TIMSCarType.TrailerCar:
                        AddNormalCarFigure(sink, key.Width);
                        break;
                }

                sink.Close();
            }

            return geometry;
        }

        private static void AddLeftCarFigure(ID2D1GeometrySink sink, float width)
        {
            sink.BeginFigure(new Vector2(0.5f, 10 + 5 + 0.5f), FigureBegin.Filled);
            sink.AddLine(new Vector2(15 + 0.5f, 10 + 0.5f));
            sink.AddLine(new Vector2(width - 0.5f, 10 + 0.5f));
            sink.AddLine(new Vector2(width - 0.5f, 10 + 19.5f));
            sink.AddLine(new Vector2(0.5f, 10 + 19.5f));
            sink.EndFigure(FigureEnd.Closed);
        }

        private static void AddNormalCarFigure(ID2D1GeometrySink sink, float width)
        {
            sink.BeginFigure(new Vector2(0.5f, 10 + 0.5f), FigureBegin.Filled);
            sink.AddLine(new Vector2(width - 0.5f, 10 + 0.5f));
            sink.AddLine(new Vector2(width - 0.5f, 10 + 19.5f));
            sink.AddLine(new Vector2(0.5f, 10 + 19.5f));
            sink.EndFigure(FigureEnd.Closed);
        }

        private static void AddGreenCarFigure(ID2D1GeometrySink sink, float width)
        {
            const float upperChamferSize = 2f;
            const float lowerChamferSize = 3f;
            const float topRoofOffset = 8f;
            sink.BeginFigure(new Vector2(0.5f, topRoofOffset + 0.5f - (lowerChamferSize - upperChamferSize)),
                FigureBegin.Filled);
            sink.AddLine(new Vector2(0.5f + upperChamferSize,
                topRoofOffset + 0.5f - (lowerChamferSize - upperChamferSize) - upperChamferSize));
            sink.AddLine(new Vector2(width - 0.5f - upperChamferSize,
                topRoofOffset + 0.5f - (lowerChamferSize - upperChamferSize) - upperChamferSize));
            sink.AddLine(new Vector2(width - 0.5f, topRoofOffset + 0.5f - (lowerChamferSize - upperChamferSize)));
            sink.AddLine(new Vector2(width - 0.5f, 10 + 21.5f));
            const float offset = 10;
            sink.AddLine(new Vector2(width - 0.5f - offset, 10 + 21.5f));
            sink.AddLine(new Vector2(width - 0.5f - offset - lowerChamferSize, 10 + 21.5f + lowerChamferSize));
            sink.AddLine(new Vector2(0.5f + offset + lowerChamferSize, 10 + 21.5f + lowerChamferSize));
            sink.AddLine(new Vector2(0.5f + offset, 10 + 21.5f));
            sink.AddLine(new Vector2(0.5f, 10 + 21.5f));
            sink.EndFigure(FigureEnd.Closed);
        }

        private static void AddRightCarFigure(ID2D1GeometrySink sink, float width)
        {
            sink.BeginFigure(new Vector2(width - 0.5f, 10 + 5 + 0.5f), FigureBegin.Filled);
            sink.AddLine(new Vector2(width - 0.5f, 10 + 19.5f));
            sink.AddLine(new Vector2(0.5f, 10 + 19.5f));
            sink.AddLine(new Vector2(0.5f, 10 + 0.5f));
            sink.AddLine(new Vector2(width - 15.5f, 10 + 0.5f));
            sink.EndFigure(FigureEnd.Closed);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);

            var carType = CarType.Value;
            var pantoGraphType = PantoGraphType.Value;
            var bgColor = BackgroundColor.Value;
            var strokeColor = StrokeColor.Value;
            var width = _width.Value;

            var bakerKey = new CarBakerKey(carType, pantoGraphType, bgColor, strokeColor, width);
            var baker = Context.GetBakerCache().GetOrCreateBaker<TIMSCarWidget, CarBakerKey>(
                Context,
                bakerKey,
                BakerPrescaleMode.AutoCubic
            );

            baker.BakeAndDraw(SelfRelativeDirtyBounds,
                () => DrawContent(carType, pantoGraphType, bgColor, strokeColor, width));
        }

        private void DrawContent(TIMSCarType carType, TIMSCarPantoGraphType pantoGraphType, Color4? bgColor,
            Color4 strokeColor, float width)
        {
            var geomKey = new CarGeometryKey(carType, width, _spec.IsCarReverseArrangement);
            var geometry = GetOrCreateGeometry(geomKey);
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            DrawWheel(carType, width);
            Context.CommonBrush.Color = bgColor ?? MonitorColors.TIMSScreenBackground;
            Context.DeviceContext.FillGeometry(geometry, Context.CommonBrush);
            Context.CommonBrush.Color = strokeColor;
            Context.DeviceContext.DrawGeometry(geometry, Context.CommonBrush);
            DrawPantoGraph(carType, pantoGraphType, strokeColor, width);
            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }

        private void DrawWheel(TIMSCarType carType, float width)
        {
            Context.CommonBrush.Color =
                carType != TIMSCarType.MotorCar ? MonitorColors.TIMSTitleGrey : MonitorColors.White;
            const float offset = 8;
            var leftEllipse = new Ellipse(new Vector2(offset, 34), 3.5f, 3.5f);
            var rightEllipse = new Ellipse(new Vector2(width - offset, 34), 3.5f, 3.5f);
            Context.DeviceContext.DrawEllipse(leftEllipse, Context.CommonBrush);
            Context.DeviceContext.DrawEllipse(rightEllipse, Context.CommonBrush);
            if (carType == TIMSCarType.MotorCar)
            {
                Context.DeviceContext.FillEllipse(leftEllipse, Context.CommonBrush);
                Context.DeviceContext.FillEllipse(rightEllipse, Context.CommonBrush);
            }
        }

        private void DrawPantoGraph(TIMSCarType carType, TIMSCarPantoGraphType pantoGraphType, Color4 strokeColor,
            float width)
        {
            if (carType != TIMSCarType.MotorCar || pantoGraphType == TIMSCarPantoGraphType.None) return;
            Context.CommonBrush.Color = strokeColor;
            if (!_spec.IsPantoGraphReversed)
            {
                Context.DeviceContext.DrawLine(
                    new Vector2(width - 9, 10),
                    new Vector2(width - 4, 5),
                    Context.CommonBrush
                );

                Context.DeviceContext.DrawLine(
                    new Vector2(width - 4, 6),
                    new Vector2(width - 9, 1),
                    Context.CommonBrush
                );
                if (pantoGraphType == TIMSCarPantoGraphType.WithYellowRect)
                {
                    Context.CommonBrush.Color = Colors.Yellow;
                    Context.DeviceContext.FillRectangle(new RectangleF(1.5f, 7.5f, 9, 2), Context.CommonBrush);
                }
            }
            else
            {
                Context.DeviceContext.DrawLine(
                    new Vector2(9, 10),
                    new Vector2(4, 5),
                    Context.CommonBrush
                );
                Context.DeviceContext.DrawLine(
                    new Vector2(4, 6),
                    new Vector2(9, 1),
                    Context.CommonBrush
                );
                if (pantoGraphType == TIMSCarPantoGraphType.WithYellowRect)
                {
                    Context.CommonBrush.Color = Colors.Yellow;
                    Context.DeviceContext.FillRectangle(new RectangleF(width - 0.5f - 9, 7.5f, 9, 2),
                        Context.CommonBrush);
                }
            }
        }

        private readonly struct CarGeometryKey : IEquatable<CarGeometryKey>
        {
            public readonly TIMSCarType CarType;
            public readonly float Width;
            public readonly bool IsCarReverseArrangement;

            public CarGeometryKey(TIMSCarType carType, float width, bool isCarReverseArrangement)
            {
                CarType = carType;
                Width = width;
                IsCarReverseArrangement = isCarReverseArrangement;
            }

            public bool Equals(CarGeometryKey other)
            {
                return CarType == other.CarType &&
                       Math.Abs(Width - other.Width) < Epsilons.FloatEpsilon &&
                       IsCarReverseArrangement == other.IsCarReverseArrangement;
            }

            public override bool Equals(object obj)
            {
                return obj is CarGeometryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(CarType, Width, IsCarReverseArrangement);
            }
        }

        private readonly struct CarBakerKey : IEquatable<CarBakerKey>
        {
            private readonly TIMSCarType _carType;
            private readonly TIMSCarPantoGraphType _pantoGraphType;
            private readonly Color4? _backgroundColor;
            private readonly Color4 _strokeColor;
            private readonly float _width;

            public CarBakerKey(TIMSCarType carType, TIMSCarPantoGraphType pantoGraphType, Color4? backgroundColor,
                Color4 strokeColor, float width)
            {
                _carType = carType;
                _pantoGraphType = pantoGraphType;
                _backgroundColor = backgroundColor;
                _strokeColor = strokeColor;
                _width = width;
            }

            public bool Equals(CarBakerKey other)
            {
                return _carType == other._carType &&
                       _pantoGraphType == other._pantoGraphType &&
                       Nullable.Equals(_backgroundColor, other._backgroundColor) &&
                       _strokeColor.Equals(other._strokeColor) &&
                       Math.Abs(_width - other._width) < Epsilons.FloatEpsilon;
            }

            public override bool Equals(object obj)
            {
                return obj is CarBakerKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_carType, _pantoGraphType, _backgroundColor, _strokeColor, _width);
            }
        }
    }

    public enum TIMSCarType
    {
        FirstCar,
        LastCar,
        MotorCar,
        TrailerCar,
        GreenCar
    }

    public enum TIMSCarPantoGraphType
    {
        None = 0,
        Has,
        WithYellowRect
    }
}