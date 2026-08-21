using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Needles
{
    public class VerticalNeedle : Widget
    {
        private const float Offset = 1;
        private const float Size = 3;
        private readonly Baker _baker;
        private readonly CommandRecorder _bottomRightInnerShadowRecorder;
        private readonly ImageInnerShadowEffect _bottomRightShadowEffect;
        private readonly Color4 _color;
        private readonly CommandRecorder _dropShadowRecorder;
        private readonly DropShadow[] _dropShadows;
        private readonly float _height;
        private readonly InnerShadowEffectChain _innerShadowEffectChain;
        private readonly ResourceSlot<ID2D1PathGeometry> _mainGeometry;

        public VerticalNeedle(RenderContext context, float x = 0, float y = 0, float height = 17,
            Color4 color = default,
            bool enableTopLeftInnerShadow = false,
            byte bottomRightInnerShadowAlpha = 175,
            float innerShadowBlur = 0
        ) : base(context, x, y)
        {
            _height = height;
            _color = color;
            _baker = new Baker(context);
            RegisterResource(_baker);
            _bottomRightInnerShadowRecorder = new CommandRecorder(context);
            RegisterResource(_bottomRightInnerShadowRecorder);
            _dropShadowRecorder = new CommandRecorder(context);
            RegisterResource(_dropShadowRecorder);
            _innerShadowEffectChain = new InnerShadowEffectChain(Context.DeviceContext);
            RegisterResource(_innerShadowEffectChain);
            if (enableTopLeftInnerShadow)
                _innerShadowEffectChain.AddOffsetShadows(new[] { Shadows.NormalNeedleInnerTopLeft });
            _bottomRightShadowEffect =
                _innerShadowEffectChain.AddImageShadow(new ImageInnerShadow(innerShadowBlur,
                    new Color4(0, 0, 0, bottomRightInnerShadowAlpha)));
            _dropShadows = new[] { new DropShadow(Offset - 1, Size, 1, 1, new Color4(0, 0, 0, 100)) };
            RectPartWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _mainGeometry = WatchResource(BuildMainGeometry);
            WatchEffect(() =>
            {
                _dropShadowRecorder.Invalidate();
                _bottomRightInnerShadowRecorder.Invalidate();
                _baker.Refresh();
            }, RectPartWidth);
        }

        public PropertySlot<float> RectPartWidth { get; }

        public float FullWidth => RectPartWidth + _height / 2;

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var shadowWidth = Shadows.GaugeNeedleDrop.MaxSpread;
                var rect = new RectangleF(0, -_height / 2, FullWidth, _height);
                rect.Inflate(shadowWidth, shadowWidth);
                return rect;
            }
        }

        private ID2D1PathGeometry BuildMainGeometry()
        {
            var width = RectPartWidth.Value;
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(new Vector2(0, -_height / 2), FigureBegin.Filled);
                sink.AddLine(new Vector2(width, -_height / 2));
                sink.AddLine(new Vector2(width + _height / 2, 0));
                sink.AddLine(new Vector2(width, _height / 2));
                sink.AddLine(new Vector2(0, _height / 2));
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            return geometry;
        }

        private void RecordInnerShadowCommandList()
        {
            var width = RectPartWidth.Value;
            using (var innerShadowGeometry = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = innerShadowGeometry.Open())
                {
                    sink.BeginFigure(new Vector2(Offset, _height / 2), FigureBegin.Filled);
                    sink.AddLine(new Vector2(Offset + Size, _height / 2 - Size));
                    sink.AddLine(new Vector2(width, _height / 2 - Size));
                    sink.AddLine(new Vector2(width + _height, _height / 2 - Size - _height));
                    sink.AddLine(new Vector2(width + _height, _height / 2 + 8));
                    sink.AddLine(new Vector2(Offset, _height / 2 + 8));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillGeometry(innerShadowGeometry, Context.CommonBrush);
            }
        }

        private void RecordDropShadowCommandList()
        {
            var width = RectPartWidth.Value;
            using (var dropShadowGeometry = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = dropShadowGeometry.Open())
                {
                    sink.BeginFigure(new Vector2(0, -_height / 2), FigureBegin.Filled);
                    sink.AddLine(new Vector2(width, -_height / 2));
                    sink.AddLine(new Vector2(width + _height / 2, 0));
                    sink.AddLine(new Vector2(width, _height / 2));
                    sink.AddLine(new Vector2(Size, _height / 2));
                    sink.AddLine(new Vector2(0, _height / 2 - Size));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillGeometry(dropShadowGeometry, Context.CommonBrush);
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds.SnapToPixels(), DrawContent);
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds.SnapToPixels(), DrawContent);
        }

        private void DrawContent()
        {
            var innerShadowCommandList = _bottomRightInnerShadowRecorder.Record(RecordInnerShadowCommandList);
            var dropShadowCommandList = _dropShadowRecorder.Record(RecordDropShadowCommandList);
            _bottomRightShadowEffect.Update(innerShadowCommandList);
            Context.DropShadowProcessor.DrawDropShadows(_dropShadows, dropShadowCommandList);
            Context.InnerShadowProcessor.DrawWithInnerShadows(_innerShadowEffectChain, () =>
            {
                Context.CommonBrush.Color = _color;
                Context.DeviceContext.FillGeometry(_mainGeometry.Value, Context.CommonBrush);
            });
        }
    }
}