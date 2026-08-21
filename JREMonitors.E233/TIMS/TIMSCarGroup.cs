using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCarGroup : Widget<TIMSFormationViewModel>
    {
        private readonly Row _row;

        public TIMSCarGroup(RenderContext context, TIMSVehicleSpec spec, bool showArrow, float leftSpacing = 0,
            float rightSpacing = 0) :
            base(context)
        {
            ViewModel = new TIMSFormationViewModel(spec);
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                AllCars[i] = new TIMSCarWidget(context, spec);
                AllCars[i].PreferredWidth.Value = LayoutLength.Flex();
                AllCars[i].StrokeColor.Value = spec.OverrideCarStrokeColor?.ToColor4() ?? MonitorColors.TIMSTitleGrey;
                AllCars[i].IsVisible.Value = false;
            }

            var children = new List<Widget>();
            if (showArrow)
            {
                var leftArrow = new Arrow(context, false);
                var rightArrow = new Arrow(context, true);
                leftArrow.IsVisible.Bind(CreateComputed(() => ViewModel.VehicleDirection == TIMSVehicleDirection.Left));
                rightArrow.IsVisible.Bind(
                    CreateComputed(() => ViewModel.VehicleDirection == TIMSVehicleDirection.Right));
                children.Add(leftArrow);
                children.AddRange(AllCars);
                children.Add(rightArrow);
            }
            else
            {
                children.AddRange(AllCars);
            }

            _row = new Row(context,
                fallbackFlexUnitWidth: GetUnitWidth(spec),
                widgetSpacing: 1,
                widgets: children,
                positionSnapToPixels: true);
            AnchorX = CreateComputed(() =>
            {
                var centerX = (LeftSpacing + (800 - RightSpacing)) / 2;
                return centerX - _row.TotalWidth / 2;
            });
            _row.X.Bind(AnchorX);
            AddChild(_row);
            FirstCarX = CreateComputed(() => _row.X + AllCars[0].X);
            LeftSpacing = CreatePropertySlot(DirtyType.Layout, leftSpacing);
            RightSpacing = CreatePropertySlot(DirtyType.Layout, rightSpacing);
            WatchEffect(EffectPhase.State, () =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec == null) return;
                var carCount = formationSpec.CarCount;

                for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
                {
                    var isVisible = i < carCount;
                    AllCars[i].IsVisible.Value = isVisible;
                    if (!isVisible) continue;
                    var carIdx = spec.GetCarIndex(carCount, i);
                    var carSpec = formationSpec[carIdx];
                    AllCars[i].CarType.Value = carSpec.CarType;
                    AllCars[i].PantoGraphType.Value = carSpec.PantoGraphType;
                    AllCars[i].CarNumber.Value = carSpec.CarNumber;
                }
            });
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public PropertySlot<float> LeftSpacing { get; }
        public PropertySlot<float> RightSpacing { get; }
        public Computed<float> AnchorX { get; }
        public PropertySlot<float> AnchorY => _row.Y;
        public Computed<float> FirstCarX { get; }

        public TIMSCarWidget[] AllCars { get; } = new TIMSCarWidget[TIMSFormationSpec.MaxCarCount];
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public static float GetUnitWidth(TIMSVehicleSpec spec)
        {
            var maxFormationCarCount = spec.MaxFormationCarCount;
            if (maxFormationCarCount <= 10) return 56;
            if (maxFormationCarCount <= 12) return 50;
            return 40;
        }

        private class Arrow : Widget, ILayoutable
        {
            private const float StartY = 15;
            private const float StartX = 0;
            private readonly bool _isRight;
            private readonly Baker _leftBaker;
            private readonly ID2D1PathGeometry _leftGeometry;
            private readonly Baker _rightBaker;
            private readonly ID2D1PathGeometry _rightGeometry;

            public Arrow(RenderContext context, bool isRight) : base(context)
            {
                _isRight = isRight;
                _leftGeometry = CreateLeftGeometry();
                RegisterResource(_leftGeometry);
                _rightGeometry = CreateRightGeometry();
                RegisterResource(_rightGeometry);
                _leftBaker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_leftBaker);
                _rightBaker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_rightBaker);
            }

            public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, 14, TIMSCarWidget.Height);

            public LayoutLength PreferredWidth { get; } = LayoutLength.Absolute(14);
            public LayoutLength PreferredHeight { get; } = LayoutLength.Absolute(TIMSCarWidget.Height);
            public float MarginWidth => 6;
            public float MarginHeight => 0;
            public bool SkipArrangeWhenHidden => false;
            public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
            public bool IncludeInTotalMajorDimensionSizeWhenHidden => true;

            public void SetLayoutSize(float width, float height)
            {
            }

            private ID2D1PathGeometry CreateLeftGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(StartX + 14.5f, StartY + 4.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 4.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 0.5f));
                    sink.AddLine(new Vector2(StartX + 0.5f, StartY + 7.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 14.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 11.5f));
                    sink.AddLine(new Vector2(StartX + 14.5f, StartY + 11.5f));
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
                    sink.BeginFigure(new Vector2(StartX + 0.5f, StartY + 4.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 4.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 0.5f));
                    sink.AddLine(new Vector2(StartX + 14.5f, StartY + 7.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 14.5f));
                    sink.AddLine(new Vector2(StartX + 7.5f, StartY + 11.5f));
                    sink.AddLine(new Vector2(StartX + 0.5f, StartY + 11.5f));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            protected override void OnStaticWarmUp(float totalScale)
            {
                var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                _leftBaker.Bake(SelfRelativeDirtyBounds, DrawLeft);
                _rightBaker.Bake(SelfRelativeDirtyBounds, DrawRight);
                Context.DeviceContext.AntialiasMode = oldAntialiasMode;
            }

            private void DrawLeft()
            {
                Context.CommonBrush.Color = MonitorColors.White;
                Context.DeviceContext.FillGeometry(_leftGeometry, Context.CommonBrush);
            }

            private void DrawRight()
            {
                Context.CommonBrush.Color = MonitorColors.White;
                Context.DeviceContext.FillGeometry(_rightGeometry, Context.CommonBrush);
            }

            protected override void OnDraw(float totalScale)
            {
                base.OnDraw(totalScale);
                if (_isRight)
                    _rightBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawRight);
                else
                    _leftBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawLeft);
            }
        }
    }
}