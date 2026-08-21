using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts
{
    public class Divider : Widget, ILayoutable
    {
        private readonly AntialiasMode _antialiasMode;
        private readonly float _borderRadius;
        private readonly Func<float?> _customRefreshSpeed;

        public Divider(
            RenderContext context,
            LayoutLength width,
            LayoutLength height,
            Color4 color,
            float marginWidth = 0,
            float borderRadius = 0,
            Func<float?> refreshSpeed = null,
            AntialiasMode antialiasMode = AntialiasMode.PerPrimitive
        ) : base(context)
        {
            _borderRadius = borderRadius;
            _antialiasMode = antialiasMode;
            PreferredWidth = width;
            PreferredHeight = height;
            MarginWidth = marginWidth;
            _customRefreshSpeed = refreshSpeed;
            Color = CreatePropertySlot(DirtyType.Visual, color);
        }

        public PropertySlot<Color4> Color { get; }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, Width, Height);
        public override float? RefreshSpeed => _customRefreshSpeed?.Invoke();
        public float Width { get; private set; }
        public float Height { get; private set; }

        public LayoutLength PreferredWidth { get; }
        public LayoutLength PreferredHeight { get; }
        public float MarginWidth { get; }
        public float MarginHeight => 0;
        public bool SkipArrangeWhenHidden => true;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;

        public void SetLayoutSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        protected override void OnDraw(float totalScale)
        {
            Context.CommonBrush.Color = Color;
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = _antialiasMode;
            Context.DeviceContext.FillRoundedRectangle(
                new RoundedRectangle(SelfRelativeDirtyBounds, _borderRadius, _borderRadius), Context.CommonBrush);
            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }
    }
}