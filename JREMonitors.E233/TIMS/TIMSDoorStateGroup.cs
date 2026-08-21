using System;
using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Services.Car;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSDoorStateGroup : Widget<TIMSDoorStateGroupViewModel>
    {
        private readonly TIMSSideDoorStateGroup _bottomDoorStateGroup;
        private readonly TIMSCarGroup _carGroup;
        private readonly Hint _hint;
        private readonly TIMSSideDoorStateGroup _topDoorStateGroup;

        public TIMSDoorStateGroup(RenderContext context, TIMSVehicleSpec spec, float y = 0, float leftSpacing = 0,
            float rightSpacing = 0) : base(context, y: y)
        {
            ViewModel = new TIMSDoorStateGroupViewModel();
            _carGroup = new TIMSCarGroup(context, spec, true, leftSpacing, rightSpacing);
            _carGroup.AnchorY.Value = 50;
            var rectWidth = new Signal<float>(spec.MaxFormationCarCount > 12 ? 8 : 9);
            _hint = new Hint(context, spec, 0, rectWidth, new Signal<float>(400));
            AddChild(_hint);
            _topDoorStateGroup = new TIMSSideDoorStateGroup(context, spec, true, 20, rectWidth);
            _topDoorStateGroup.AnchorX.Bind(_carGroup.FirstCarX);
            AddChild(_topDoorStateGroup);
            AddChild(_carGroup);
            _bottomDoorStateGroup = new TIMSSideDoorStateGroup(context, spec, false, 100, rectWidth);
            _bottomDoorStateGroup.AnchorX.Bind(_carGroup.FirstCarX);
            AddChild(_bottomDoorStateGroup);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
        public Computed<float> FirstCarX => _carGroup.FirstCarX;
        public PropertySlot<float> LeftSpacing => _carGroup.LeftSpacing;
        public PropertySlot<float> RightSpacing => _carGroup.RightSpacing;

        protected override void OnArrangeLayout(bool ignoreHidden)
        {
            ArrangeChild(_hint, ignoreHidden);
            ArrangeChild(_carGroup, ignoreHidden);
            ArrangeChild(_topDoorStateGroup, ignoreHidden);
            ArrangeChild(_bottomDoorStateGroup, ignoreHidden);
        }

        private class Hint : Widget
        {
            private const float Height = 20;

            public Hint(RenderContext context, TIMSVehicleSpec spec, float y, IValueSignal<float> rectWidth,
                IValueSignal<float> anchorX) : base(
                context, y: y)
            {
                var rectBounds = CreateComputed(() => new RectangleF(0, 0, rectWidth.Value, Height));
                var children = new List<Widget>
                {
                    CreateRect(rectBounds),
                    CreateText("：閉\u3000"),
                    CreateRect(rectBounds, Colors.Yellow),
                    CreateText("：開\u3000"),
                    CreateRect(rectBounds, Colors.Red),
                    CreateText("：故障\u3000"),
                    CreateRect(rectBounds, strokeColor: Colors.Red),
                    CreateText("：ドア被バックアップ中" + (spec.SupportsPartialDoorOperation ? "\u3000" : ""))
                };
                if (spec.SupportsPartialDoorOperation)
                {
                    var partialDoorOperationIcon = CreateRect(rectBounds, strokeColor: Colors.LawnGreen);
                    partialDoorOperationIcon.IncludeInTotalMajorDimensionSizeWhenVisible.Value = false;
                    children.Add(partialDoorOperationIcon);
                    var partialDoorOperationText =
                        CreateText("：一部開扉選択実施中");
                    partialDoorOperationText.IncludeInTotalMajorDimensionSizeWhenVisible.Value = false;
                    children.Add(partialDoorOperationText);
                }

                var row = new Row(context,
                    widgets: children, positionSnapToPixels: true,
                    rowHorizontalAlignment: 0.5f, widgetVerticalAlignment: 0.5f, rowVerticalAlignment: 0.5f);
                row.X.Bind(CreateComputed(() =>
                    spec.SupportsPartialDoorOperation ? anchorX.Value - 40 : anchorX.Value));
                AddChild(row);
            }

            public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
            protected override bool MergeChildrenDirtyBounds => true;

            private BoundsDrawerWidget CreateRect(Computed<RectangleF> bounds, Color4 backgroundColor = default,
                Color4? strokeColor = null)
            {
                var rectWidget = new BoundsDrawerWidget(Context, null,
                    backgroundColor: backgroundColor,
                    strokeColor: strokeColor ?? MonitorColors.TIMSTitleGrey, strokeWidth: 1, useBaker: true,
                    bakerPrescaleMode: BakerPrescaleMode.AutoCubic,
                    backgroundAntialiasMode: AntialiasMode.Aliased);
                rectWidget.TargetBounds.Bind(bounds);
                return rectWidget;
            }

            private BoundsDrawerWidget CreateText(string text)
            {
                return new BoundsDrawerWidget(Context, this.CreateTIMSTextDrawer(text),
                    contentColor: MonitorColors.TIMSTitleGrey);
            }
        }
    }

    public class TIMSDoorStateGroupViewModel : ViewModel
    {
        private IDoorStateService _doorStateService;

        public Signal<int> CarCount { get; } = new Signal<int>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _doorStateService = dataHub.Get<IDoorStateService>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            CarCount.Value = _doorStateService.CarCount;
        }
    }
}