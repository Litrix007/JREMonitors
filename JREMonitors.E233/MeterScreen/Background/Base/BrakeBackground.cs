using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Background.Base
{
    public class BrakeBackground : Widget
    {
        public const float CircleRatio = 1.5f;
        public const float Height = 325;
        public const float ShortWidth = BcBackground.ShortWidth;
        public const float LongWidth = 97;
        public const float SpacingY = 17;
        public const int MaxBrake = 8;
        public const float PieceHeight = (Height - (MaxBrake - 1) * SpacingY) / MaxBrake;
        public const float FullySpacingY = PieceHeight + SpacingY;
        private static readonly RectangleF BakeBounds = new RectangleF(-LongWidth, -Height, LongWidth, Height);
        private readonly Baker _baker;
        private readonly ID2D1PathGeometry _geometry;

        public BrakeBackground(RenderContext context, float x, float y) : base(context, x, y)
        {
            _geometry = CreateBrakeGeometry(Context.D2D1Factory, false, MaxBrake);
            RegisterResource(_geometry);
            _baker = new Baker(Context);
            RegisterResource(_baker);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

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
            Context.CommonBrush.Color = MonitorColors.Recessed;
            Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.RecessedInner,
                () => { Context.DeviceContext.FillGeometry(_geometry, Context.CommonBrush); });
        }

        public static ID2D1PathGeometry CreateBrakeGeometry(ID2D1Factory factory, bool createCircle, int brake,
            int start = 0)
        {
            if (brake < 0 || brake > MaxBrake) throw new ArgumentOutOfRangeException(nameof(brake));
            if (start < 0 || start >= brake) throw new ArgumentOutOfRangeException(nameof(start));
            var geometry = factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                for (var i = start; i < brake; i++)
                {
                    var bottomY = -i * FullySpacingY;
                    var pieceShortWidth = MathHelper.Lerp(ShortWidth, LongWidth, -bottomY / Height);
                    var pieceLongWidth = MathHelper.Lerp(ShortWidth, LongWidth, (-bottomY + PieceHeight) / Height);
                    sink.BeginFigure(new Vector2(0, bottomY), FigureBegin.Filled);
                    sink.AddLine(new Vector2(-pieceShortWidth, bottomY));
                    sink.AddLine(new Vector2(-pieceLongWidth, bottomY - PieceHeight));
                    sink.AddLine(new Vector2(0, bottomY - PieceHeight));
                    sink.EndFigure(FigureEnd.Closed);
                    if (!createCircle || i != brake - 1) continue;
                    var centerX = -MathHelper.Lerp(pieceShortWidth, pieceLongWidth, 0.5f) / 2;
                    var centerY = bottomY - PieceHeight / 2;
                    sink.AddCircleFigure(centerX, centerY, PieceHeight * CircleRatio / 2);
                }

                sink.Close();
            }

            return geometry;
        }
    }
}