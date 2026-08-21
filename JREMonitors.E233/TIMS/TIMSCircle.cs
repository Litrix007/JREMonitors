using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCircle : Widget, ILayoutable
    {
        public const float CircleRadius = 8.5f;

        public TIMSCircle(RenderContext context, Color4? color = null) : base(context)
        {
            Color = CreatePropertySlot(DirtyType.Visual, color ?? MonitorColors.White);
        }

        public override RectangleF SelfRelativeDirtyBounds =>
            new RectangleF(0, 0, CircleRadius * 2 + 1, CircleRadius * 2 + 1);

        public PropertySlot<Color4> Color { get; }
        public LayoutLength PreferredWidth { get; } = LayoutLength.Absolute(CircleRadius * 2 + 1);
        public LayoutLength PreferredHeight { get; } = LayoutLength.Absolute(CircleRadius * 2 + 1);
        public float MarginWidth { get; set; } = 0;
        public float MarginHeight { get; set; } = 0;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;
        public bool SkipArrangeWhenHidden => true;

        public void SetLayoutSize(float width, float height)
        {
        }

        protected override void OnDraw(float totalScale)
        {
            Context.GetBakerCache().GetOrCreateBaker<TIMSCircle, Color4>(Context, Color, BakerPrescaleMode.AutoCubic)
                .BakeAndDraw(SelfRelativeDirtyBounds, () =>
                {
                    var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                    Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                    Context.CommonBrush.Color = Color;
                    Context.DeviceContext.FillEllipse(
                        new Ellipse(new Vector2(CircleRadius, CircleRadius), CircleRadius, CircleRadius),
                        Context.CommonBrush);
                    Context.DeviceContext.AntialiasMode = oldAntialiasMode;
                });
        }
    }
}