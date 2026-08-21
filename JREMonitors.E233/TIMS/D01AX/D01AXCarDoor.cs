using System;
using System.Drawing;
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

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXCarDoor : Widget, ILayoutable
    {
        private readonly Color4 _openBackgroundColor;
        private readonly Color4 _openTextColor;
        private readonly Signal<string> _text;
        private readonly BitmapScaleDrawer _textDrawer;
        private readonly PropertySlot<float> _width;

        public D01AXCarDoor(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            _openBackgroundColor = spec.D01AXSpec.OverrideDoorOpenBackgroundColor?.ToColor4() ?? Colors.Yellow;
            _openTextColor = spec.D01AXSpec.OverrideDoorOpenTextColor?.ToColor4() ?? MonitorColors.TIMSScreenBackground;
            PreferredWidth = CreatePropertySlot(DirtyType.Layout, LayoutLength.Absolute(0));
            MarginWidth = CreatePropertySlot<float>(DirtyType.Layout);
            IsOpen = CreatePropertySlot<bool>(DirtyType.Visual);
            _width = CreatePropertySlot<float>(DirtyType.Visual);
            _text = new Signal<string>("\u3000");
            _textDrawer = this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(_text)),
                horizontalAlignment: 0.5f, verticalAlignment: 0.5f);
            RegisterResource(_textDrawer);
        }

        public PropertySlot<LayoutLength> PreferredWidth { get; }
        public PropertySlot<float> MarginWidth { get; }
        public PropertySlot<bool> IsOpen { get; }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, _width.Value, 20);

        LayoutLength ILayoutable.PreferredWidth => PreferredWidth.Value;
        LayoutLength ILayoutable.PreferredHeight { get; } = LayoutLength.Absolute(20);
        float ILayoutable.MarginWidth => MarginWidth.Value;
        float ILayoutable.MarginHeight => 0;
        bool ILayoutable.SkipArrangeWhenHidden => true;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenVisible => true;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden => false;

        public void SetLayoutSize(float width, float height)
        {
            _width.Value = width;
        }

        protected override void OnDraw(float totalScale)
        {
            var isOpen = IsOpen.Value;
            var key = new CarDoorCacheKey(_width.Value, isOpen);
            var baker = Context.GetBakerCache().GetOrCreateBaker<D01AXCarDoor, CarDoorCacheKey>(
                Context,
                key,
                BakerPrescaleMode.AutoCubic
            );
            baker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(isOpen));
        }

        private void Draw(bool isOpen)
        {
            var antialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            var rect = SelfRelativeDirtyBounds;
            rect.Inflate(-0.5f, -0.5f);
            if (isOpen)
            {
                Context.CommonBrush.Color = _openBackgroundColor;
                Context.DeviceContext.FillRectangle(rect, Context.CommonBrush);
            }

            Context.CommonBrush.Color = isOpen ? MonitorColors.White : MonitorColors.TIMSTitleGrey;
            Context.DeviceContext.DrawRectangle(rect, Context.CommonBrush);
            Context.DeviceContext.AntialiasMode = antialiasMode;
            _text.Value = isOpen ? "開" : "閉";
            _textDrawer.Draw(SelfRelativeDirtyBounds, isOpen ? _openTextColor : MonitorColors.TIMSTitleGrey);
        }

        private readonly struct CarDoorCacheKey : IEquatable<CarDoorCacheKey>
        {
            private readonly float _width;
            private readonly bool _isOpen;

            public CarDoorCacheKey(float width, bool isOpen)
            {
                _width = width;
                _isOpen = isOpen;
            }

            public bool Equals(CarDoorCacheKey other)
            {
                return Math.Abs(_width - other._width) < Epsilons.FloatEpsilon && _isOpen == other._isOpen;
            }

            public override bool Equals(object obj)
            {
                return obj is CarDoorCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_width, _isOpen);
            }
        }
    }
}