using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Widgets
{
    public class BoundsDrawerWidget : Widget, ILayoutable
    {
        private readonly Baker _baker;
        private readonly PropertySlot<LayoutLength> _preferredHeight;

        private readonly PropertySlot<LayoutLength> _preferredWidth;
        private readonly PropertySlot<RectangleF> _selfRelativeDirtyBounds;
        private readonly ResourceSlot<ID2D1StrokeStyle> _strokeStyle;
        private readonly bool _warmUp;

        public BoundsDrawerWidget(
            RenderContext context,
            IContentMeasurableBoundsDrawer drawer,
            bool useBaker = false,
            BakerPrescaleMode? bakerPrescaleMode = null,
            RectangleF targetBounds = default,
            Color4 contentColor = default,
            float x = 0,
            float y = 0,
            bool warmUp = true,
            Color4 backgroundColor = default,
            Color4 strokeColor = default,
            float strokeWidth = 0,
            float strokeDashWidth = 0,
            float strokeGapWidth = 0,
            AntialiasMode backgroundAntialiasMode = AntialiasMode.PerPrimitive,
            LayoutLength? preferredWidth = null,
            LayoutLength? preferredHeight = null,
            bool autoSyncTargetBoundsToLayout = true
        ) : base(context, x, y)
        {
            Drawer = drawer;
            if (Drawer != null) RegisterResource(Drawer);
            if (useBaker)
            {
                _baker = new Baker(context, bakerPrescaleMode);
                RegisterResource(_baker);
            }

            _warmUp = warmUp;
            TargetBounds = CreatePropertySlot(DirtyType.Visual, targetBounds);
            ContentColor = CreatePropertySlot(DirtyType.Visual, contentColor);
            BackgroundColor = CreatePropertySlot(DirtyType.Visual, backgroundColor);
            StrokeColor = CreatePropertySlot(DirtyType.Visual, strokeColor);
            StrokeWidth = CreatePropertySlot(DirtyType.Visual, strokeWidth);
            StrokeDashWidth = CreatePropertySlot(DirtyType.Visual, strokeDashWidth);
            StrokeGapWidth = CreatePropertySlot(DirtyType.Visual, strokeGapWidth);
            BackgroundAntialiasMode = CreatePropertySlot(DirtyType.Visual, backgroundAntialiasMode);
            MarginWidth = CreatePropertySlot<float>(DirtyType.Layout);
            MarginHeight = CreatePropertySlot<float>(DirtyType.Layout);
            SkipArrangeWhenHidden = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenVisible = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenHidden = CreatePropertySlot(DirtyType.Layout, false);
            CustomPreferredWidth = CreatePropertySlot(DirtyType.Layout, preferredWidth);
            CustomPreferredHeight = CreatePropertySlot(DirtyType.Layout, preferredHeight);
            AutoSyncTargetBounds = CreatePropertySlot(DirtyType.Layout, autoSyncTargetBoundsToLayout);
            _selfRelativeDirtyBounds = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                if (Drawer == null) return TargetBounds.Value;
                var target = TargetBounds.Value;
                var contentBounds = Drawer.GetContentBounds(target);
                return target.IsEmpty || contentBounds.IsEmpty
                    ? target.IsEmpty ? contentBounds : target
                    : RectangleF.Union(target, contentBounds);
            }));
            _preferredWidth = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                var customPreferredWidth = CustomPreferredWidth.Value;
                if (customPreferredWidth.HasValue) return customPreferredWidth.Value;
                return LayoutLength.Absolute(_selfRelativeDirtyBounds.Value.Width);
            }));
            _preferredHeight = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                var customPreferredHeight = CustomPreferredHeight.Value;
                if (customPreferredHeight.HasValue) return customPreferredHeight.Value;
                return LayoutLength.Absolute(_selfRelativeDirtyBounds.Value.Height);
            }));

            var trackable = Drawer as ITrackable;
            WatchEffect(EffectPhase.Commit, () =>
            {
                trackable?.Track();
                ContentColor.Track();
                BackgroundColor.Track();
                StrokeColor.Track();
                StrokeWidth.Track();
                StrokeDashWidth.Track();
                StrokeGapWidth.Track();
                BackgroundAntialiasMode.Track();
                _selfRelativeDirtyBounds.Track();
                _baker?.Refresh();
            });

            _strokeStyle = WatchResource(() =>
            {
                var sw = StrokeWidth.Value;
                var dw = StrokeDashWidth.Value;
                if (sw <= 0 || dw <= 0) return null;
                var gapWidth = StrokeGapWidth.Value <= 0 ? dw : StrokeGapWidth.Value;
                var strokeProperties = new StrokeStyleProperties
                {
                    StartCap = CapStyle.Flat,
                    EndCap = CapStyle.Flat,
                    DashCap = CapStyle.Flat,
                    LineJoin = LineJoin.Miter,
                    DashStyle = DashStyle.Custom,
                    DashOffset = 0.0f
                };
                float[] dashes = { dw / sw, gapWidth / sw };
                return Context.D2D1Factory.CreateStrokeStyle(strokeProperties, dashes);
            });
        }

        public override RectangleF SelfRelativeDirtyBounds => _selfRelativeDirtyBounds;
        public IContentMeasurableBoundsDrawer Drawer { get; }
        public PropertySlot<RectangleF> TargetBounds { get; }
        public PropertySlot<Color4> ContentColor { get; }
        public PropertySlot<Color4> BackgroundColor { get; }
        public PropertySlot<Color4> StrokeColor { get; }
        public PropertySlot<float> StrokeWidth { get; }
        public PropertySlot<float> StrokeDashWidth { get; }
        public PropertySlot<float> StrokeGapWidth { get; }
        public PropertySlot<AntialiasMode> BackgroundAntialiasMode { get; }
        public PropertySlot<float> MarginWidth { get; }
        public PropertySlot<float> MarginHeight { get; }
        public PropertySlot<LayoutLength?> CustomPreferredWidth { get; }
        public PropertySlot<LayoutLength?> CustomPreferredHeight { get; }
        public PropertySlot<bool> AutoSyncTargetBounds { get; }
        public PropertySlot<bool> SkipArrangeWhenHidden { get; set; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenVisible { get; set; }

        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenHidden { get; set; }
        LayoutLength ILayoutable.PreferredWidth => _preferredWidth;
        LayoutLength ILayoutable.PreferredHeight => _preferredHeight;
        float ILayoutable.MarginWidth => MarginWidth;
        float ILayoutable.MarginHeight => MarginHeight;
        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangeWhenHidden;

        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenVisible => IncludeInTotalMajorDimensionSizeWhenVisible;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden => IncludeInTotalMajorDimensionSizeWhenHidden;

        public void SetLayoutSize(float width, float height)
        {
            if (AutoSyncTargetBounds.Value && (width > 0 || height > 0))
            {
                var current = TargetBounds.Value;
                var newW = width > 0 ? width : current.Width;
                var newH = height > 0 ? height : current.Height;
                if (Math.Abs(current.Width - newW) > Epsilons.FloatEpsilon ||
                    Math.Abs(current.Height - newH) > Epsilons.FloatEpsilon)
                    TargetBounds.Value = new RectangleF(current.X, current.Y, newW, newH);
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            if (!_warmUp) return;
            Draw();
        }

        protected override void OnDraw(float totalScale)
        {
            Draw();
        }

        private void Draw()
        {
            if (_baker != null)
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, DrawContent);
            else
                DrawContent();
        }

        private void DrawContent()
        {
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            if (oldAntialiasMode != BackgroundAntialiasMode)
                Context.DeviceContext.AntialiasMode = BackgroundAntialiasMode;
            var bgColor = BackgroundColor.Value;
            if (bgColor.A > 0)
            {
                Context.CommonBrush.Color = bgColor;
                Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
            }

            var strokeWidth = StrokeWidth.Value;
            if (strokeWidth > 0)
            {
                Context.CommonBrush.Color = StrokeColor.Value;

                var dashWidth = StrokeDashWidth.Value;
                var style = _strokeStyle?.Value;
                if (dashWidth > 0 && style != null)
                {
                    var rect = SelfRelativeDirtyBounds;
                    var halfStroke = strokeWidth / 2f;
                    var left = (float)Math.Floor(rect.X) + halfStroke;
                    var top = (float)Math.Floor(rect.Y) + halfStroke;
                    var right = (float)Math.Floor(rect.Right) - halfStroke;
                    var bottom = (float)Math.Floor(rect.Bottom) - halfStroke;
                    var dc = Context.DeviceContext;
                    dc.DrawLine(new Vector2(left, top), new Vector2(right, top), Context.CommonBrush, strokeWidth,
                        style);
                    dc.DrawLine(new Vector2(left, bottom), new Vector2(right, bottom), Context.CommonBrush, strokeWidth,
                        style);
                    dc.DrawLine(new Vector2(left, top), new Vector2(left, bottom), Context.CommonBrush, strokeWidth,
                        style);
                    dc.DrawLine(new Vector2(right, top), new Vector2(right, bottom), Context.CommonBrush, strokeWidth,
                        style);
                }
                else
                {
                    var strokeRect = SelfRelativeDirtyBounds;
                    var halfStroke = strokeWidth / 2f;
                    strokeRect.Inflate(-halfStroke, -halfStroke);
                    Context.DeviceContext.DrawRectangle(strokeRect, Context.CommonBrush, strokeWidth);
                }
            }

            if (oldAntialiasMode != BackgroundAntialiasMode)
                Context.DeviceContext.AntialiasMode = oldAntialiasMode;
            Drawer?.Draw(TargetBounds.Value, ContentColor.Value);
        }
    }
}