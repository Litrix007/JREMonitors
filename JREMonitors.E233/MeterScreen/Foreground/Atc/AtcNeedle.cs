using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Atc
{
    public class AtcNeedle : Widget
    {
        private const float Width = 25;
        private const float Height = 28;
        private static readonly float HalfSize = (float)Math.Sqrt(Width * Width + Height * Height);

        private static readonly OffsetInnerShadow[] OnInnerShadows =
        {
            Shadows.NormalNeedleInnerTopLeft,
            new OffsetInnerShadow(-2f, -2f, 1, Shadows.NormalNeedleInnerBottomRight.Color)
        };

        private readonly Baker _baker;
        private readonly ID2D1PathGeometry _geometry;
        private readonly float _radius;

        public AtcNeedle(RenderContext context, float x, float y, float radius, bool on) : base(context, x, y)
        {
            _radius = radius;
            _geometry = CreateGeometry();
            RegisterResource(_geometry);
            _baker = new Baker(Context);
            RegisterResource(_baker);
            Degree = CreatePropertySlot<float>(DirtyType.Layout);
            On = CreatePropertySlot(DirtyType.Visual, on);
            OnColor = CreateRelayPropertySlot(MonitorColors.NormalGreen);
            WatchEffect(EffectPhase.Commit, () =>
            {
                Degree.Track();
                if (On) OnColor.Track();

                _baker.Refresh();
            });
        }

        public PropertySlot<float> Degree { get; }
        public PropertySlot<bool> On { get; }
        public PropertySlot<Color4> OnColor { get; }

        private Vector2 ApexPos => Vector2.Zero.ForwardByAngle(Degree, _radius);

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var pos = ApexPos;
                return new RectangleF(pos.X - HalfSize, pos.Y - HalfSize, HalfSize * 2, HalfSize * 2);
            }
        }

        private ID2D1PathGeometry CreateGeometry()
        {
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(new Vector2(_radius, 0), FigureBegin.Filled);
                sink.AddLine(new Vector2(_radius + Height, -Width / 2));
                sink.AddLine(new Vector2(_radius + Height, Width / 2));
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            return geometry;
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds,
                () =>
                {
                    Context.CommonBrush.Color = On ? OnColor : MonitorColors.Recessed;
                    Context.InnerShadowProcessor.DrawWithInnerShadows(
                        On ? OnInnerShadows : Shadows.RecessedInner,
                        () =>
                        {
                            var oldTransform = Context.DeviceContext.Transform;
                            Context.DeviceContext.Transform =
                                RenderHelper.CreateRotationMatrixInDegrees(Degree) * oldTransform;
                            Context.DeviceContext.FillGeometry(_geometry, Context.CommonBrush);
                            Context.DeviceContext.Transform = oldTransform;
                        }
                    );
                });
        }
    }
}