using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Services.Car;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSideDoorWidget : Widget, ILayoutable
    {
        private const float Height = 20;
        private readonly Baker _baker;
        private readonly Computed<float> _marginWidth;
        private readonly IValueSignal<float> _unitWidth;
        private readonly PropertySlot<float> _width;

        public TIMSideDoorWidget(RenderContext context, TIMSVehicleSpec spec, IValueSignal<float> unitWidth) :
            base(context)
        {
            _unitWidth = unitWidth;
            _width = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                var uw = unitWidth.Value;
                return uw + (uw - 1) * 3 + 1;
            }));
            _marginWidth = CreateComputed(() => TIMSCarGroup.GetUnitWidth(spec) - _width);
            DoorStates = CreateReactiveList<DoorState>(DirtyType.Visual, capacity: 4);
            _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
            RegisterResource(_baker);
            WatchEffect(() => _baker.Refresh(), _unitWidth, DoorStates);
        }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var bounds = new RectangleF(0, 0, _width, Height);
                bounds.Inflate(2, 2);
                return bounds;
            }
        }

        public ReactiveList<DoorState> DoorStates { get; }

        public LayoutLength PreferredWidth => LayoutLength.Absolute(_width);
        public LayoutLength PreferredHeight => LayoutLength.Absolute(Height);
        float ILayoutable.MarginWidth => _marginWidth;
        public float MarginHeight => 0;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;
        public bool SkipArrangeWhenHidden => true;

        public void SetLayoutSize(float width, float height)
        {
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            _baker.Alloc(new RectangleF(0, 0, 30, Height));
        }

        protected override void OnDraw(float totalScale)
        {
            if (Context.DeviceContext.GetMaxWorldScale() > 1)
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
            else
                Draw();
        }

        private void Draw()
        {
            var count = DoorStates.Count;
            if (count != 4 && count != 2) return;

            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            Context.CommonBrush.Color = MonitorColors.TIMSTitleGrey;
            var unitWidth = _unitWidth.Value;
            for (var i = 0; i < 4; i++)
            {
                if (count == 2 && i > 0 && i < 3) continue;
                var x = i * (unitWidth - 1);
                Context.DeviceContext.DrawRectangle(
                    new RectangleF(x + 0.5f, 0.5f, unitWidth - 1, Height - 1),
                    Context.CommonBrush
                );
            }

            Context.CommonBrush.Color = Colors.Yellow;
            for (var i = 0; i < 4; i++)
            {
                if (count == 2 && i > 0 && i < 3) continue;
                var idx = count == 2 ? i == 0 ? 0 : 1 : i;
                if (DoorStates[idx] == DoorState.Closed) continue;
                var x = i * (unitWidth - 1);
                Context.DeviceContext.FillRectangle(
                    new RectangleF(x + 1, 1, unitWidth - 2, Height - 2),
                    Context.CommonBrush
                );
            }

            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }
    }
}