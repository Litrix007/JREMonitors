using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.TickMarks
{
    public class VerticalTickMarks : Widget
    {
        public const float DefaultMajorTickMarkStrokeWidth = 3.5f;
        public const float DefaultMinorTickMarkStrokeWidth = 3f;
        public const float TitleOffsetBottom = -18;
        public const float TitleOffsetTop = TitleOffsetBottom - FontSizes.Title;
        public const float Height = 397;
        public const float TickFontSize = 25;
        public const float TextSpacingX = 7;
        private readonly Baker _baker;
        private readonly PropertySlot<int> _bottomMajorTickMarkWidth;
        private readonly bool _drawTitle;
        private readonly PropertySlot<int> _majorScaleCount;
        private readonly PropertySlot<float> _majorTickMarkStrokeWidth;
        private readonly PropertySlot<int> _max;
        private readonly PropertySlot<int> _min;
        private readonly PropertySlot<int> _minorScaleCount;
        private readonly PropertySlot<float> _minorTickMarkOffsetX;
        private readonly PropertySlot<float> _minorTickMarkStrokeWidth;
        private readonly PropertySlot<float> _minorTickMarkWidth;
        private readonly PropertySlot<bool> _numAlwaysCenterAlign;
        private readonly Computed<float> _scaleMajor;
        private readonly Computed<float> _scaleMinor;
        private readonly ID2D1StrokeStyle1 _strokeStyle;
        private readonly IDWriteTextFormat _tickTextFormat;
        private readonly PropertySlot<int> _topMajorTickMarkWidth;

        public VerticalTickMarks(RenderContext context, float x, float y, bool drawTitle = true)
            : base(context, x, y)
        {
            _drawTitle = drawTitle;
            _baker = new Baker(context);
            RegisterResource(_baker);
            _strokeStyle = context.D2D1Factory.CreateStrokeStyle(GeometryHelper.RoundStrokeStyleProperties);
            RegisterResource(_strokeStyle);
            _tickTextFormat = GetOrCreateTickTextFormat(context);
            _bottomMajorTickMarkWidth = CreatePropertySlot<int>(DirtyType.Visual);
            _topMajorTickMarkWidth = CreatePropertySlot<int>(DirtyType.Visual);
            _majorScaleCount = CreatePropertySlot<int>(DirtyType.Visual);
            _majorTickMarkStrokeWidth = CreatePropertySlot(DirtyType.Visual, DefaultMajorTickMarkStrokeWidth);
            _max = CreatePropertySlot<int>(DirtyType.Visual);
            _min = CreatePropertySlot<int>(DirtyType.Visual);
            _minorScaleCount = CreatePropertySlot<int>(DirtyType.Visual);
            _minorTickMarkOffsetX = CreatePropertySlot<float>(DirtyType.Visual);
            _minorTickMarkStrokeWidth = CreatePropertySlot(DirtyType.Visual, DefaultMinorTickMarkStrokeWidth);
            _minorTickMarkWidth = CreatePropertySlot<float>(DirtyType.Visual);
            _numAlwaysCenterAlign = CreatePropertySlot<bool>(DirtyType.Visual);
            _scaleMajor = CreateComputed(() =>
                MajorScaleCount > 1 ? Height / MajorScaleCount : 0);
            _scaleMinor = CreateComputed(() =>
                MinorScaleCount > 0 ? _scaleMajor / MinorScaleCount : 0);
            WatchEffect(
                () => _baker.Refresh(),
                _bottomMajorTickMarkWidth,
                _topMajorTickMarkWidth,
                _majorScaleCount,
                _majorTickMarkStrokeWidth,
                _max,
                _min,
                _minorScaleCount,
                _minorTickMarkOffsetX,
                _minorTickMarkStrokeWidth,
                _minorTickMarkWidth,
                _numAlwaysCenterAlign,
                _scaleMajor,
                _scaleMinor
            );
        }

        public int MajorScaleCount
        {
            get => _majorScaleCount.Value;
            set => _majorScaleCount.Value = value;
        }

        public int MinorScaleCount
        {
            get => _minorScaleCount.Value;
            set => _minorScaleCount.Value = value;
        }

        public int TopMajorTickMarkWidth
        {
            get => _topMajorTickMarkWidth.Value;
            set => _topMajorTickMarkWidth.Value = value;
        }

        public int BottomMajorTickMarkWidth
        {
            get => _bottomMajorTickMarkWidth.Value;
            set => _bottomMajorTickMarkWidth.Value = value;
        }

        public float MinorTickMarkWidth
        {
            get => _minorTickMarkWidth.Value;
            set => _minorTickMarkWidth.Value = value;
        }

        public float MajorTickMarkStrokeWidth
        {
            get => _majorTickMarkStrokeWidth.Value;
            set => _majorTickMarkStrokeWidth.Value = value;
        }

        public float MinorTickMarkStrokeWidth
        {
            get => _minorTickMarkStrokeWidth.Value;
            set => _minorTickMarkStrokeWidth.Value = value;
        }

        public float MinorTickMarkOffsetX
        {
            get => _minorTickMarkOffsetX.Value;
            set => _minorTickMarkOffsetX.Value = value;
        }

        public int Min
        {
            get => _min.Value;
            set => _min.Value = value;
        }

        public int Max
        {
            get => _max.Value;
            set => _max.Value = value;
        }

        public bool NumAlwaysCenterAlign
        {
            get => _numAlwaysCenterAlign.Value;
            set => _numAlwaysCenterAlign.Value = value;
        }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var shadowWidth = Shadows.TickMarkDrop.Max(shadow => shadow.MaxSpread);
                var maxWidth = Math.Max(TopMajorTickMarkWidth, BottomMajorTickMarkWidth);
                var rect = RectangleF.FromLTRB(
                    -maxWidth,
                    TitleOffsetTop + TickFontSize / 2,
                    TextSpacingX + 50,
                    Height);
                rect.Inflate(shadowWidth, shadowWidth);
                return rect;
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            base.OnStaticWarmUp(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        protected override void OnDraw(float totalScale)
        {
            base.OnDraw(totalScale);
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        private void Draw()
        {
            if (MajorScaleCount == 0) return;
            var scaleMajor = _scaleMajor.Value;
            var scaleMinor = _scaleMinor.Value;
            Context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop, () =>
            {
                for (var i = 0; i <= MajorScaleCount; i++)
                {
                    var y = i * scaleMajor;
                    var w = MathHelper.Lerp(TopMajorTickMarkWidth, BottomMajorTickMarkWidth, y / Height);
                    Context.CommonBrush.Color = MonitorColors.White;
                    Context.DeviceContext.DrawLine(new Vector2(0, y), new Vector2(-w, y), Context.CommonBrush,
                        MajorTickMarkStrokeWidth, _strokeStyle);
                    var num = Min == Max ? Min : Max - (Max - Min) * i / MajorScaleCount;
                    Context.DeviceContext.DrawDynamicText(Context.DwFactory, num.ToString(), TextSpacingX, y,
                        _tickTextFormat,
                        Context.CommonBrush, 0, i == MajorScaleCount ? 0.9f :
                        NumAlwaysCenterAlign ? 0.45f : 0.9f - 0.55f * (0.9f - (float)i / MajorScaleCount),
                        Fonts.ItalicDegrees);
                    if (i == MajorScaleCount) continue;
                    for (var j = 1; j < MinorScaleCount; j++)
                    {
                        var my = y + j * scaleMinor;
                        var mw = MinorTickMarkWidth;
                        var color = MinorScaleCount % 2 == 0 && j == MinorScaleCount / 2
                            ? MonitorColors.White
                            : MonitorColors.MinorTickMark;
                        Context.CommonBrush.Color = color;
                        Context.DeviceContext.DrawLine(new Vector2(0 - MinorTickMarkOffsetX - mw, my),
                            new Vector2(0 - MinorTickMarkOffsetX, my), Context.CommonBrush, MinorTickMarkStrokeWidth,
                            _strokeStyle);
                    }
                }
            });
            if (_drawTitle) DrawTitle(0, 0, Context);
        }

        public static IDWriteTextFormat GetOrCreateTickTextFormat(RenderContext context, bool bold = false)
        {
            return context.FontManager.GetOrCreateFormat(
                Fonts.CenturyGothic,
                TickFontSize,
                fontWeight: bold ? FontWeight.SemiBold : FontWeight.Normal);
        }

        public static void DrawTitle(float x, float y, RenderContext context)
        {
            context.DropShadowProcessor.DrawWithDropShadows(Shadows.TickMarkDrop, () =>
            {
                context.CommonBrush.Color = MonitorColors.White;
                var format = context.FontManager.GetOrCreateFormat(
                    Fonts.CenturyGothic,
                    FontSizes.Title * 4 / 5,
                    fontWeight: FontWeight.SemiBold);
                context.DeviceContext.DrawDynamicText(
                    context.DwFactory,
                    "kPa",
                    x + TextSpacingX,
                    y + TitleOffsetBottom,
                    format,
                    context.CommonBrush,
                    0,
                    1f,
                    Fonts.ItalicDegrees,
                    fontSizes: new[]
                    {
                        new FontSize
                        {
                            Index = 1,
                            Length = 1,
                            Size = FontSizes.Title
                        }
                    });
            });
        }
    }
}