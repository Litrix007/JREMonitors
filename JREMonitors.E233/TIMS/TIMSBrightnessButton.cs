using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSBrightnessButton : Widget
    {
        private static readonly float[] StepValues = { 1f, 0.9f, 0.8f, 0.7f, 0.5f };

        public TIMSBrightnessButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height
        ) : base(context, pos.X, pos.Y)
        {
            AddChild(new CycleDrawerIconSwitchButton(
                context, Vector2.Zero, width, height,
                context.TIMS().FooterIconButtonStyle,
                new BrightnessIconDrawer(context),
                StepValues,
                () => Context.DisplayController.Brightness,
                val => Context.DisplayController.Brightness = val
            ));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private class BrightnessIconDrawer : LevelIconDrawer
        {
            private static readonly PropertyKey GeometryCacheKey =
                new PropertyKey($"{nameof(BrightnessIconDrawer)}GeometryCache");

            private readonly ID2D1StrokeStyle1 _strokeStyle;

            public BrightnessIconDrawer(RenderContext context) : base(context, new SizeF(15, 15))
            {
                _strokeStyle = context.D2D1Factory.CreateStrokeStyle(GeometryHelper.SquareStrokeStyleProperties);
            }

            private ID2D1PathGeometry GetOrCreateGeometry(int key)
            {
                var cache = Context.GetResourceCache<int, ID2D1PathGeometry>(GeometryCacheKey);
                return cache.GetOrCreate(key, (factory: Context.D2D1Factory, key),
                    s => BuildGeometry(s.factory, s.key));
            }

            protected override void DrawIcon(float originX, float originY, Color4 color, int level)
            {
                var dc = Context.DeviceContext;
                var oldAntialias = dc.AntialiasMode;
                dc.AntialiasMode = AntialiasMode.Aliased;
                Context.CommonBrush.Color = color;
                var oldTransform = dc.Transform;
                dc.Transform = Matrix3x2.CreateTranslation(originX, originY) * oldTransform;
                dc.FillRectangle(new RectangleF(7, 0, 1, 2), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(7, 13, 1, 2), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(0, 7, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(13, 7, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(2, 2, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(3, 3, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(12, 2, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(11, 3, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(2, 12, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(3, 11, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(12, 12, 1, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(11, 11, 1, 1), Context.CommonBrush);
                var outlineGeometry = GetOrCreateGeometry(0);
                if (outlineGeometry != null) dc.DrawGeometry(outlineGeometry, Context.CommonBrush, 1f, _strokeStyle);

                if (level > 0 && level <= 4)
                {
                    var levelGeometry = GetOrCreateGeometry(level);
                    if (levelGeometry != null) dc.FillGeometry(levelGeometry, Context.CommonBrush);
                }

                dc.Transform = oldTransform;
                dc.AntialiasMode = oldAntialias;
            }

            private static ID2D1PathGeometry BuildGeometry(ID2D1Factory factory, int key)
            {
                if (key == 0) return BuildOutlineGeometry(factory);
                return BuildLevelGeometry(factory, key);
            }

            private static ID2D1PathGeometry BuildOutlineGeometry(ID2D1Factory factory)
            {
                var geometry = factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(6.5f, 3.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(8.5f, 3.5f));
                    sink.AddLine(new Vector2(11.5f, 6.5f));
                    sink.AddLine(new Vector2(11.5f, 8.5f));
                    sink.AddLine(new Vector2(8.5f, 11.5f));
                    sink.AddLine(new Vector2(6.5f, 11.5f));
                    sink.AddLine(new Vector2(3.5f, 8.5f));
                    sink.AddLine(new Vector2(3.5f, 6.5f));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            private static ID2D1PathGeometry BuildLevelGeometry(ID2D1Factory factory, int level)
            {
                var geometry = factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    switch (level)
                    {
                        case 1:
                            sink.BeginFigure(new Vector2(7f, 4f), FigureBegin.Filled);
                            sink.AddLine(new Vector2(9.2f, 4f));
                            sink.AddLine(new Vector2(10f, 5f));
                            sink.AddLine(new Vector2(11.2f, 6f));
                            sink.AddLine(new Vector2(11.2f, 9f));
                            sink.AddLine(new Vector2(10f, 10f));
                            sink.AddLine(new Vector2(9.2f, 11f));
                            sink.AddLine(new Vector2(7f, 11f));
                            sink.AddLine(new Vector2(8f, 10f));
                            sink.AddLine(new Vector2(9f, 9f));
                            sink.AddLine(new Vector2(9f, 6f));
                            sink.AddLine(new Vector2(8f, 5f));
                            sink.EndFigure(FigureEnd.Closed);
                            break;
                        case 2:
                            sink.BeginFigure(new Vector2(7f, 4f), FigureBegin.Filled);
                            sink.AddLine(new Vector2(9.2f, 4f));
                            sink.AddLine(new Vector2(10f, 5f));
                            sink.AddLine(new Vector2(11.2f, 6f));
                            sink.AddLine(new Vector2(11.2f, 9f));
                            sink.AddLine(new Vector2(10f, 10f));
                            sink.AddLine(new Vector2(9.2f, 11f));
                            sink.AddLine(new Vector2(7f, 11f));
                            sink.EndFigure(FigureEnd.Closed);
                            break;
                        case 3:
                            sink.BeginFigure(new Vector2(7f, 4f), FigureBegin.Filled);
                            sink.AddLine(new Vector2(9.2f, 4f));
                            sink.AddLine(new Vector2(10f, 5f));
                            sink.AddLine(new Vector2(11.2f, 6f));
                            sink.AddLine(new Vector2(11.2f, 9f));
                            sink.AddLine(new Vector2(10f, 10f));
                            sink.AddLine(new Vector2(9.2f, 11f));
                            sink.AddLine(new Vector2(7.0f, 11f));
                            sink.AddLine(new Vector2(6.2f, 10f));
                            sink.AddLine(new Vector2(5.2f, 9f));
                            sink.AddLine(new Vector2(5.2f, 6f));
                            sink.AddLine(new Vector2(6.2f, 5f));
                            sink.EndFigure(FigureEnd.Closed);
                            break;
                        case 4:
                            sink.BeginFigure(new Vector2(6.0f, 4f), FigureBegin.Filled);
                            sink.AddLine(new Vector2(9.2f, 4f));
                            sink.AddLine(new Vector2(10f, 5f));
                            sink.AddLine(new Vector2(11.2f, 6f));
                            sink.AddLine(new Vector2(11.2f, 9f));
                            sink.AddLine(new Vector2(10f, 10f));
                            sink.AddLine(new Vector2(9.2f, 11f));
                            sink.AddLine(new Vector2(6.0f, 11f));
                            sink.AddLine(new Vector2(5.2f, 10f));
                            sink.AddLine(new Vector2(4.2f, 9f));
                            sink.AddLine(new Vector2(4.2f, 6f));
                            sink.AddLine(new Vector2(5.2f, 5f));
                            sink.EndFigure(FigureEnd.Closed);
                            break;
                    }

                    sink.Close();
                }

                return geometry;
            }

            public override void Dispose()
            {
                base.Dispose();
                _strokeStyle?.Dispose();
            }
        }
    }
}