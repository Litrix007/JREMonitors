using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Atc
{
    public class AtcAbsoluteStopCross : Widget
    {
        private const float LineHalfWidth = 20;
        private const float StrokeWidth = 5;
        private static readonly float AxisHalfWidth = MathHelper.Sqrt(LineHalfWidth * LineHalfWidth / 2);
        private readonly Baker _activeBaker;
        private readonly Baker _inactiveBaker;

        public AtcAbsoluteStopCross(RenderContext context, float x, float y) : base(context, x, y)
        {
            _activeBaker = new Baker(context);
            RegisterResource(_activeBaker);
            _inactiveBaker = new Baker(context);
            RegisterResource(_inactiveBaker);
            Stopped = CreatePropertySlot<bool>(DirtyType.Visual);
        }

        public PropertySlot<bool> Stopped { get; }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var rect = RectangleF.Empty;
                rect.Inflate(LineHalfWidth, LineHalfWidth);
                return rect;
            }
        }

        public override float? RefreshSpeed => RefreshSpeeds.Fast;

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _inactiveBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawInactive);
            _activeBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawActive);
        }

        private void DrawInactive()
        {
            Context.CommonBrush.Color = MonitorColors.Recessed;
            Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.RecessedInner, () =>
            {
                Context.DeviceContext.DrawLine(new Vector2(-AxisHalfWidth, -AxisHalfWidth),
                    new Vector2(AxisHalfWidth, AxisHalfWidth),
                    Context.CommonBrush, StrokeWidth);
                Context.DeviceContext.DrawLine(new Vector2(-AxisHalfWidth, AxisHalfWidth),
                    new Vector2(AxisHalfWidth, -AxisHalfWidth),
                    Context.CommonBrush, StrokeWidth);
            });
        }

        private void DrawActive()
        {
            Context.CommonBrush.Color = Colors.Red;
            Context.DeviceContext.DrawLine(new Vector2(-AxisHalfWidth, -AxisHalfWidth),
                new Vector2(AxisHalfWidth, AxisHalfWidth), Context.CommonBrush, StrokeWidth);
            Context.DeviceContext.DrawLine(new Vector2(-AxisHalfWidth, AxisHalfWidth),
                new Vector2(AxisHalfWidth, -AxisHalfWidth), Context.CommonBrush, StrokeWidth);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            if (Stopped)
                _activeBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawActive);
            else
                _inactiveBaker.BakeAndDraw(SelfRelativeDirtyBounds, DrawInactive);
        }
    }
}