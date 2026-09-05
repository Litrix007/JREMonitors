using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Lamps
{
    public class Lamp : Widget, ILayoutable, IExpandableElement<bool>
    {
        public const float DefaultOffBorderRadius = 2;
        public const float DefaultOnBorderRadius = 4;
        public const float DefaultOnInnerShadowWidth = 3;
        private readonly PropertySlot<float> _baseHeight;
        private readonly PropertySlot<float> _baseWidth;
        private readonly ImageInnerShadowEffect _bottomRightInnerShadowEffect;
        private readonly ImageInnerShadow _bottomRightInnerShadowImage;
        private readonly CommandRecorder _bottomRightInnerShadowRecorder;
        private readonly IBoundsDrawer _boundsDrawer;
        private readonly PropertySlot<float> _currentBottomExpansion;
        private readonly PropertySlot<float> _currentLeftExpansion;
        private readonly PropertySlot<float> _currentRightExpansion;
        private readonly PropertySlot<float> _currentTopExpansion;
        private readonly bool _expandToFillGaps;
        private readonly bool _hiddenWhenOff;
        private readonly Color4 _offBackgroundColor;
        private readonly LampBackgroundType _offBackgroundType;
        private readonly Color4 _offBorderColor;
        private readonly float _offBorderRadius;
        private readonly float _offBorderWidth;
        private readonly Color4 _offOutlineColor;
        private readonly float _offOutlineRadius;
        private readonly float _offOutlineWidth;
        private readonly Baker _offTextBaker;
        private readonly Color4 _offTextColor;
        private readonly Color4 _onBackgroundColor;
        private readonly float _onBorderRadius;
        private readonly DropShadow[] _onDropShadows;
        private readonly float _onInnerShadowAlphaRatio;
        private readonly InnerShadowEffectChain _onInnerShadowChain;
        private readonly float _onInnerShadowWidth;
        private readonly Color4 _onOutlineColor;
        private readonly float _onOutlineRadius;
        private readonly float _onOutlineWidth;
        private readonly float _onStaticExtensionHeight;
        private readonly float _onStaticExtensionWidth;
        private readonly Baker _onTextBaker;
        private readonly Color4 _onTextColor;
        private readonly InterpolationMode? _overrideTextInterpolationMode;
        private readonly PropertySlot<LayoutLength> _preferredHeightSlot;
        private readonly PropertySlot<LayoutLength> _preferredWidthSlot;
        private readonly ImageInnerShadowEffect _topLeftInnerShadowEffect;
        private readonly ImageInnerShadow _topLeftInnerShadowImage;
        private readonly CommandRecorder _topLeftInnerShadowRecorder;
        private ID2D1PathGeometry _onGeometryBottomRight;

        public Lamp(
            RenderContext context,
            IBoundsDrawer boundsDrawer = null,
            float x = 0,
            float y = 0,
            LayoutLength? preferredWidth = null,
            LayoutLength? preferredHeight = null,
            Color4? offBorderColor = null,
            float offBorderWidth = 0,
            float offBorderRadius = DefaultOffBorderRadius,
            float offOutlineWidth = 0,
            float offOutlineRadius = 0,
            Color4? offBackgroundColor = null,
            Color4? offOutlineColor = null,
            Color4? offTextColor = null,
            LampBackgroundType offBackgroundType = LampBackgroundType.Solid,
            float onBorderRadius = DefaultOnBorderRadius,
            float onOutlineWidth = 0,
            float onOutlineRadius = 0,
            float onStaticExtensionWidth = 0,
            float onStaticExtensionHeight = 0,
            Color4? onBackgroundColor = null,
            Color4? onOutlineColor = null,
            Color4? onTextColor = null,
            float onInnerShadowWidth = DefaultOnInnerShadowWidth,
            float onInnerShadowAlphaRatio = 1,
            DropShadow[] onDropShadows = null,
            bool expandToFillGaps = false,
            bool hiddenWhenOff = false,
            bool truncateBottomRightInnerShadow = true,
            InterpolationMode? overrideTextInterpolationMode = null
        ) : base(context, x, y)
        {
            _boundsDrawer = boundsDrawer;
            RegisterResource(boundsDrawer);
            if (!(boundsDrawer is IContentHashable))
            {
                _offTextBaker = new Baker(context);
                RegisterResource(_offTextBaker);
                _onTextBaker = new Baker(context);
                RegisterResource(_onTextBaker);
            }

            _offBorderColor = offBorderColor ?? default;
            _offBorderWidth = offBorderWidth;
            _offBorderRadius = offBorderRadius;
            _offOutlineWidth = offOutlineWidth;
            _offOutlineRadius = offOutlineRadius;
            _offBackgroundColor = offBackgroundColor ?? Colors.Transparent;
            _offOutlineColor = offOutlineColor ?? MonitorColors.White;
            _offTextColor = offTextColor ?? MonitorColors.MeterTitleGrey;
            _offBackgroundType = offBackgroundType;
            _onBorderRadius = onBorderRadius;
            _onOutlineWidth = onOutlineWidth;
            _onOutlineRadius = onOutlineRadius;
            _onStaticExtensionWidth = onStaticExtensionWidth;
            _onStaticExtensionHeight = onStaticExtensionHeight;
            MaxDynamicExpansion = onInnerShadowWidth;
            _expandToFillGaps = expandToFillGaps;
            _onBackgroundColor = onBackgroundColor ?? MonitorColors.White;
            _onOutlineColor = onOutlineColor ?? default;
            _onTextColor = onTextColor ?? Colors.Black;
            _hiddenWhenOff = hiddenWhenOff;
            _onInnerShadowWidth = onInnerShadowWidth;
            _onInnerShadowAlphaRatio = onInnerShadowAlphaRatio;
            _topLeftInnerShadowImage = new ImageInnerShadow(1f,
                new Color4(1f, 1f, 1f, 100f * onInnerShadowAlphaRatio / 255f));
            _bottomRightInnerShadowImage = new ImageInnerShadow(1f,
                new Color4(0f, 0f, 0f, 180f * onInnerShadowAlphaRatio / 255f));
            _onInnerShadowChain = new InnerShadowEffectChain(context.DeviceContext);
            RegisterResource(_onInnerShadowChain);
            _topLeftInnerShadowEffect = _onInnerShadowChain.AddImageShadow(_topLeftInnerShadowImage);
            _topLeftInnerShadowRecorder = new CommandRecorder(context);
            RegisterResource(_topLeftInnerShadowRecorder);
            _bottomRightInnerShadowEffect = _onInnerShadowChain.AddImageShadow(_bottomRightInnerShadowImage);
            _bottomRightInnerShadowRecorder = new CommandRecorder(context);
            RegisterResource(_bottomRightInnerShadowRecorder);
            _onDropShadows = onDropShadows ?? Array.Empty<DropShadow>();
            _preferredWidthSlot = CreatePropertySlot(DirtyType.Layout, preferredWidth ?? LayoutLength.Absolute(0));
            _preferredHeightSlot = CreatePropertySlot(DirtyType.Layout, preferredHeight ?? LayoutLength.Absolute(0));
            _baseWidth = CreatePropertySlot(DirtyType.Visual, PreferredWidth.AbsoluteValue);
            _baseHeight = CreatePropertySlot(DirtyType.Visual, PreferredHeight.AbsoluteValue);
            _overrideTextInterpolationMode = overrideTextInterpolationMode;
            if (truncateBottomRightInnerShadow) RefreshTruncatedBottomRightGeometry();
            On = CreatePropertySlot<bool>(DirtyType.Visual);
            SkipArrangeWhenHidden = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenVisible = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenHidden = CreatePropertySlot(DirtyType.Layout, false);
            _currentLeftExpansion = CreatePropertySlot(DirtyType.Visual, -1f);
            _currentRightExpansion = CreatePropertySlot(DirtyType.Visual, -1f);
            _currentTopExpansion = CreatePropertySlot(DirtyType.Visual, -1f);
            _currentBottomExpansion = CreatePropertySlot(DirtyType.Visual, -1f);
            WatchEffect(() =>
            {
                _offTextBaker?.Refresh();
                _onTextBaker?.Refresh();
                _topLeftInnerShadowRecorder.Invalidate();
                _bottomRightInnerShadowRecorder.Invalidate();
            }, _baseWidth, _baseHeight);

            WatchEffect(() =>
            {
                _offTextBaker?.Refresh();
                _onTextBaker?.Refresh();
            }, _boundsDrawer);

            if (_expandToFillGaps)
                WatchEffect(() =>
                {
                    _topLeftInnerShadowRecorder.Invalidate();
                    _bottomRightInnerShadowRecorder.Invalidate();
                }, _currentLeftExpansion, _currentRightExpansion, _currentTopExpansion, _currentBottomExpansion);

            if (truncateBottomRightInnerShadow)
                WatchEffect(EffectPhase.Commit, RefreshTruncatedBottomRightGeometry, false);
        }

        public PropertySlot<bool> On { get; }
        public PropertySlot<bool> SkipArrangeWhenHidden { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenVisible { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenHidden { get; }
        protected override IList<RectangleF> LocalClickBoundsList => new[] { BaseBounds };
        public override float? RefreshSpeed => RefreshSpeeds.Fast;
        public bool KeepOffWhenFirstRender { get; set; }

        private RectangleF BaseBounds => new RectangleF(0, 0, BaseWidth, BaseHeight);

        private RectangleF TextBounds
        {
            get
            {
                var bounds = BaseBounds;
                bounds.Inflate(-Math.Max(MaxDynamicExpansion, _offBorderWidth),
                    -Math.Max(MaxDynamicExpansion, _offBorderWidth));
                return bounds;
            }
        }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var maxLeftOffset = Math.Max(_offOutlineWidth,
                    _onStaticExtensionWidth / 2f + _onOutlineWidth + MaxDynamicExpansion);
                var maxTopOffset = Math.Max(_offOutlineWidth,
                    _onStaticExtensionHeight / 2f + _onOutlineWidth + MaxDynamicExpansion);
                var dropShadowWidth =
                    _onDropShadows.Length > 0 ? _onDropShadows.Max(shadow => shadow.MaxSpread) : 0;
                var bounds = BaseBounds;
                bounds.Inflate(dropShadowWidth + maxLeftOffset, dropShadowWidth + maxTopOffset);
                return bounds;
            }
        }

        public float BaseWidth
        {
            get => _baseWidth;
            set => _baseWidth.Value = value;
        }

        public float BaseHeight
        {
            get => _baseHeight;
            set => _baseHeight.Value = value;
        }

        public bool ExpansionState => On;
        public float MaxDynamicExpansion { get; }
        public float StaticExtensionLeft => _onStaticExtensionWidth / 2f;
        public float StaticExtensionRight => _onStaticExtensionWidth / 2f;
        public float StaticExtensionTop => _onStaticExtensionHeight / 2f;
        public float StaticExtensionBottom => _onStaticExtensionHeight / 2f;

        public void SetDynamicExpansionLimits(float left, float right, float top, float bottom)
        {
            _currentLeftExpansion.Value = left;
            _currentRightExpansion.Value = right;
            _currentTopExpansion.Value = top;
            _currentBottomExpansion.Value = bottom;
        }

        public LayoutLength PreferredWidth
        {
            get => _preferredWidthSlot;
            set => _preferredWidthSlot.Value = value;
        }

        public LayoutLength PreferredHeight
        {
            get => _preferredHeightSlot;
            set => _preferredHeightSlot.Value = value;
        }

        public float MarginWidth => _offOutlineWidth * 2;
        public float MarginHeight => _offOutlineWidth * 2;
        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangeWhenHidden;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenVisible => IncludeInTotalMajorDimensionSizeWhenVisible;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden => IncludeInTotalMajorDimensionSizeWhenHidden;

        public void SetLayoutSize(float width, float height)
        {
            if (Math.Abs(BaseWidth - width) < Epsilons.FloatEpsilon &&
                Math.Abs(BaseHeight - height) < Epsilons.FloatEpsilon) return;
            BaseWidth = width;
            BaseHeight = height;
        }

        private void RefreshTruncatedBottomRightGeometry()
        {
            _onGeometryBottomRight?.Dispose();
            GetRectPositions(out var rectX, out var rectY, out var rectW, out var rectH);
            using (var g1 = Context.D2D1Factory.CreatePathGeometry())
            using (var g2 = Context.D2D1Factory.CreateRoundedRectangleGeometry(
                       new RoundedRectangle(new RectangleF(rectX, rectY, rectW, rectH), _onBorderRadius,
                           _onBorderRadius)))
            {
                _onGeometryBottomRight = Context.D2D1Factory.CreatePathGeometry();
                using (var sink1 = g1.Open())
                {
                    var g1X = rectX + _onInnerShadowWidth;
                    var g1Y = rectY + _onInnerShadowWidth;
                    var g1W = rectW - _onInnerShadowWidth;
                    var g1H = rectH - _onInnerShadowWidth;
                    sink1.BeginFigure(new Vector2(g1X, g1Y), FigureBegin.Filled);
                    sink1.AddLine(new Vector2(g1X + g1W, g1Y));
                    sink1.AddLine(new Vector2(g1X + g1W, g1Y + g1H - _onBorderRadius));
                    sink1.AddRoundedCorner(new Vector2(g1X + g1W, g1Y + g1H - _onBorderRadius),
                        new Vector2(g1X + g1W - _onBorderRadius, g1Y + g1H), true);
                    sink1.AddLine(new Vector2(g1X, g1Y + g1H));
                    sink1.EndFigure(FigureEnd.Closed);
                    sink1.Close();
                }

                using (var sinkFinal = _onGeometryBottomRight.Open())
                {
                    g2.CombineWithGeometry(g1, CombineMode.Intersect, sinkFinal);
                    sinkFinal.Close();
                }
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            DrawOff();
            DrawOn();
        }

        protected override void OnDraw(float totalScale)
        {
            if (_offOutlineWidth > 0)
            {
                Context.CommonBrush.Color = _offOutlineColor;
                Context.DeviceContext.FillRoundedRectangle(new RoundedRectangle(new RectangleF(0 - _offOutlineWidth,
                        0 - _offOutlineWidth,
                        BaseWidth + _offOutlineWidth * 2, BaseHeight + _offOutlineWidth * 2), _offOutlineRadius,
                    _offOutlineRadius), Context.CommonBrush);
            }

            if (IsOffScreen)
            {
                DrawOff();
                DrawOn();
                return;
            }

            if ((KeepOffWhenFirstRender && IsFirstRender) || !On)
            {
                if (_hiddenWhenOff) return;
                DrawOff();
            }
            else
            {
                DrawOn();
            }
        }

        private void DrawOff()
        {
            var bakeBounds = SelfRelativeDirtyBounds;
            var key = BuildBackgroundKey(bakeBounds, false);
            var baker = Context.GetBakerCache()
                .GetOrCreateBaker<Lamp, LampBakerKey>(Context, key);
            baker.BakeAndDraw(bakeBounds, () =>
            {
                if (_offBackgroundType == LampBackgroundType.Solid)
                {
                    Context.CommonBrush.Color = _offBackgroundColor;
                    Context.DeviceContext.FillRoundedRectangle(
                        new RoundedRectangle(BaseBounds, _offBorderRadius, _offBorderRadius), Context.CommonBrush);
                }
                else
                {
                    Context.CommonBrush.Color = MonitorColors.Recessed;
                    var rect = new RoundedRectangle(BaseBounds, _offBorderRadius, _offBorderRadius);
                    if (MaxDynamicExpansion > 0)
                        Context.InnerShadowProcessor.DrawWithInnerShadows(Shadows.RecessedInner,
                            () => { Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush); });
                    else
                        Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush);
                }

                if (_offBorderWidth > 0)
                {
                    Context.CommonBrush.Color = _offBorderColor;
                    Context.DeviceContext.DrawInnerRoundedRectangle(BaseBounds, _offBorderRadius, _offBorderRadius,
                        Context.CommonBrush,
                        _offBorderWidth);
                }
            }, overrideInterpolationMode: InterpolationMode.Cubic);
            DrawDrawer(SelfRelativeDirtyBounds, _offTextColor, _offTextBaker);
        }

        private void DrawOn()
        {
            var bakeBounds = SelfRelativeDirtyBounds;
            var key = BuildBackgroundKey(bakeBounds, true);
            var baker = Context.GetBakerCache().GetOrCreateBaker<Lamp, LampBakerKey>(Context, key);
            baker.BakeAndDraw(bakeBounds, () =>
            {
                GetRectPositions(out var rectX, out var rectY, out var rectW, out var rectH);
                var outlineRect = new RoundedRectangle(
                    new RectangleF(rectX - _onOutlineWidth, rectY - _onOutlineWidth,
                        rectW + _onOutlineWidth * 2, rectH + _onOutlineWidth * 2), _onOutlineRadius, _onOutlineRadius);
                var rect = new RoundedRectangle(new RectangleF(rectX, rectY, rectW, rectH), _onBorderRadius,
                    _onBorderRadius);

                if (_onDropShadows.Length > 0)
                {
                    Context.CommonBrush.Color = Colors.Black;
                    Context.DropShadowProcessor.DrawWithDropShadows(_onDropShadows,
                        shadowAction: () =>
                        {
                            Context.DeviceContext.FillRoundedRectangle(
                                _onOutlineWidth > 0 ? outlineRect : rect, Context.CommonBrush);
                        });
                }

                if (_onOutlineWidth > 0)
                {
                    Context.CommonBrush.Color = _onOutlineColor;
                    Context.DeviceContext.FillRoundedRectangle(outlineRect, Context.CommonBrush);
                }

                _topLeftInnerShadowEffect.Update(_topLeftInnerShadowImage,
                    _topLeftInnerShadowRecorder.RecordTransformed(RecordTopLeftMaskGeometry));
                _bottomRightInnerShadowEffect.Update(_bottomRightInnerShadowImage,
                    _bottomRightInnerShadowRecorder.RecordTransformed(RecordBottomRightMaskGeometry));
                Context.CommonBrush.Color = _onBackgroundColor;
                Context.InnerShadowProcessor.DrawWithInnerShadows(_onInnerShadowChain,
                    () => Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush));
            }, overrideInterpolationMode: InterpolationMode.Cubic);
            DrawDrawer(SelfRelativeDirtyBounds, _onTextColor, _onTextBaker);
        }

        private LampBakerKey BuildBackgroundKey(RectangleF bakeBounds, bool isOn)
        {
            return new LampBakerKey(
                bakeBounds.Width, bakeBounds.Height,
                BaseWidth, BaseHeight,
                _currentLeftExpansion, _currentRightExpansion, _currentTopExpansion, _currentBottomExpansion,
                MaxDynamicExpansion,
                _onStaticExtensionWidth, _onStaticExtensionHeight,
                _expandToFillGaps,
                isOn,
                _offBackgroundType, _offBorderRadius, _offBorderWidth, _offBorderColor, _offBackgroundColor,
                _onBorderRadius, _onOutlineWidth, _onOutlineRadius, _onOutlineColor, _onBackgroundColor,
                _onInnerShadowAlphaRatio, _onDropShadows
            );
        }

        private InterpolationMode GetTextInterpolationMode()
        {
            if (_overrideTextInterpolationMode.HasValue) return _overrideTextInterpolationMode.Value;
            Context.DeviceContext.GetWorldScale(out var scaleX, out var scaleY);
            return Math.Abs(scaleX - (int)scaleX) < Epsilons.FloatEpsilon &&
                   Math.Abs(scaleY - (int)scaleY) < Epsilons.FloatEpsilon
                ? InterpolationMode.NearestNeighbor
                : InterpolationMode.Cubic;
        }

        private void DrawDrawer(RectangleF bakeBounds, Color4 color, Baker localBaker)
        {
            if (_boundsDrawer == null) return;
            var shouldSnapToPixels = GetTextInterpolationMode() == InterpolationMode.NearestNeighbor;
            var bounds = shouldSnapToPixels ? bakeBounds.SnapToPixels() : bakeBounds;
            var textBounds = shouldSnapToPixels ? TextBounds.SnapToPixels() : TextBounds;

            if (_boundsDrawer is IContentHashable hashable)
            {
                var key = new LampDrawerKey(hashable.ToSnapshot(), bounds.Size, color,
                    textBounds.Left - bounds.Left, textBounds.Top - bounds.Top,
                    textBounds.Width, textBounds.Height);
                var baker = Context.GetBakerCache().GetOrCreateBaker<Lamp, LampDrawerKey>(Context, key);
                baker.BakeAndDraw(bounds, () => _boundsDrawer.Draw(textBounds, color),
                    overrideInterpolationMode: _overrideTextInterpolationMode);
                return;
            }

            localBaker.BakeAndDraw(bounds, () => _boundsDrawer.Draw(textBounds, color),
                overrideInterpolationMode: _overrideTextInterpolationMode);
        }

        private void GetRectPositions(out float rectX, out float rectY, out float rectW, out float rectH)
        {
            var leftExp = StaticExtensionLeft + (_expandToFillGaps
                ? _currentLeftExpansion < 0 ? MaxDynamicExpansion : _currentLeftExpansion
                : 0f);
            var rightExp = StaticExtensionRight + (_expandToFillGaps
                ? _currentRightExpansion < 0 ? MaxDynamicExpansion : _currentRightExpansion
                : 0f);
            var topExp = StaticExtensionTop + (_expandToFillGaps
                ? _currentTopExpansion < 0 ? MaxDynamicExpansion : _currentTopExpansion
                : 0f);
            var bottomExp = StaticExtensionBottom + (_expandToFillGaps
                ? _currentBottomExpansion < 0 ? MaxDynamicExpansion : _currentBottomExpansion
                : 0f);

            rectX = 0 - leftExp;
            rectY = 0 - topExp;
            rectW = BaseWidth + leftExp + rightExp;
            rectH = BaseHeight + topExp + bottomExp;
        }

        private void RecordTopLeftMaskGeometry()
        {
            GetRectPositions(out var rectX, out var rectY, out var rectW, out var rectH);
            var b = _topLeftInnerShadowImage.Blur * 3.0f;

            using (var geom = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = geom.Open())
                {
                    sink.AddTopLeftInnerBevelFigure(rectW, rectH, _onInnerShadowWidth, _onBorderRadius, b);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                var oldTransform = Context.DeviceContext.Transform;
                Context.DeviceContext.Transform = Matrix3x2.CreateTranslation(rectX, rectY) * oldTransform;
                Context.DeviceContext.FillGeometry(geom, Context.CommonBrush);
                Context.DeviceContext.Transform = oldTransform;
            }
        }

        private void RecordBottomRightMaskGeometry()
        {
            GetRectPositions(out var rectX, out var rectY, out var rectW, out var rectH);
            var b = _bottomRightInnerShadowImage.Blur * 3.0f;

            using (var geom = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = geom.Open())
                {
                    sink.AddBottomRightInnerBevelFigure(rectW, rectH, _onInnerShadowWidth, _onBorderRadius, b);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                var oldTransform = Context.DeviceContext.Transform;
                Context.DeviceContext.Transform = Matrix3x2.CreateTranslation(rectX, rectY) * oldTransform;
                Context.DeviceContext.FillGeometry(geom, Context.CommonBrush);
                Context.DeviceContext.Transform = oldTransform;
            }
        }

        protected override void ClearStates(bool clearDirtyStates, bool clearRenderStates, bool parentWasUpdated)
        {
            var firstRender = IsFirstRender;
            base.ClearStates(clearDirtyStates, clearRenderStates, parentWasUpdated);
            if (KeepOffWhenFirstRender && firstRender) Invalidate(DirtyType.Visual);
        }

        protected override void OnDispose()
        {
            _onGeometryBottomRight?.Dispose();
        }

        private readonly struct LampBakerKey : IEquatable<LampBakerKey>
        {
            private readonly float _bakeWidth, _bakeHeight;
            private readonly float _baseWidth, _baseHeight;
            private readonly float _expansionLeft, _expansionRight, _expansionTop, _expansionBottom;
            private readonly float _maxDynamicExpansion;
            private readonly float _onStaticExtensionWidth, _onStaticExtensionHeight;
            private readonly bool _expandToFillGaps;
            private readonly bool _isOn;
            private readonly LampBackgroundType _offType;
            private readonly float _offBorderRadius, _offBorderWidth;
            private readonly Color4 _offBorderColor, _offBackgroundColor;
            private readonly float _onBorderRadius, _onOutlineWidth, _onOutlineRadius;
            private readonly Color4 _onOutlineColor, _onBackgroundColor;
            private readonly float _onInnerShadowAlphaRatio;
            private readonly IReadOnlyList<DropShadow> _onDropShadows;

            public LampBakerKey(
                float bakeW, float bakeH,
                float baseW, float baseH,
                float expL, float expR, float expT, float expB,
                float maxDynExp,
                float onStaticW, float onStaticH,
                bool expandToFillGaps,
                bool isOn,
                LampBackgroundType offType, float offBorderRadius, float offBorderWidth,
                Color4 offBorderColor, Color4 offBackgroundColor,
                float onBorderRadius, float onOutlineWidth, float onOutlineRadius,
                Color4 onOutlineColor, Color4 onBackgroundColor,
                float onInnerShadowAlphaRatio,
                DropShadow[] onDropShadows)
            {
                _bakeWidth = bakeW;
                _bakeHeight = bakeH;
                _baseWidth = baseW;
                _baseHeight = baseH;
                _expansionLeft = expL;
                _expansionRight = expR;
                _expansionTop = expT;
                _expansionBottom = expB;
                _maxDynamicExpansion = maxDynExp;
                _onStaticExtensionWidth = onStaticW;
                _onStaticExtensionHeight = onStaticH;
                _expandToFillGaps = expandToFillGaps;
                _isOn = isOn;
                _offType = offType;
                _offBorderRadius = offBorderRadius;
                _offBorderWidth = offBorderWidth;
                _offBorderColor = offBorderColor;
                _offBackgroundColor = offBackgroundColor;
                _onBorderRadius = onBorderRadius;
                _onOutlineWidth = onOutlineWidth;
                _onOutlineRadius = onOutlineRadius;
                _onOutlineColor = onOutlineColor;
                _onBackgroundColor = onBackgroundColor;
                _onInnerShadowAlphaRatio = onInnerShadowAlphaRatio;
                _onDropShadows = onDropShadows ?? Array.Empty<DropShadow>();
            }

            public bool Equals(LampBakerKey o)
            {
                if (!FloatEquals(_bakeWidth, o._bakeWidth) || !FloatEquals(_bakeHeight, o._bakeHeight)) return false;
                if (!FloatEquals(_baseWidth, o._baseWidth) || !FloatEquals(_baseHeight, o._baseHeight)) return false;
                if (!FloatEquals(_expansionLeft, o._expansionLeft) || !FloatEquals(_expansionRight, o._expansionRight))
                    return false;
                if (!FloatEquals(_expansionTop, o._expansionTop) || !FloatEquals(_expansionBottom, o._expansionBottom))
                    return false;
                if (!FloatEquals(_maxDynamicExpansion, o._maxDynamicExpansion)) return false;
                if (_expandToFillGaps != o._expandToFillGaps || _isOn != o._isOn) return false;

                if (_isOn)
                {
                    if (!FloatEquals(_onStaticExtensionWidth, o._onStaticExtensionWidth) ||
                        !FloatEquals(_onStaticExtensionHeight, o._onStaticExtensionHeight))
                        return false;
                    if (!FloatEquals(_onBorderRadius, o._onBorderRadius)) return false;
                    if (!FloatEquals(_onOutlineWidth, o._onOutlineWidth) ||
                        !FloatEquals(_onOutlineRadius, o._onOutlineRadius))
                        return false;
                    if (!_onOutlineColor.Equals(o._onOutlineColor) ||
                        !_onBackgroundColor.Equals(o._onBackgroundColor))
                        return false;
                    if (!FloatEquals(_onInnerShadowAlphaRatio, o._onInnerShadowAlphaRatio)) return false;

                    var shadowsA = _onDropShadows;
                    var shadowsB = o._onDropShadows;
                    if (shadowsA.Count != shadowsB.Count) return false;
                    for (var i = 0; i < shadowsA.Count; i++)
                        if (!shadowsA[i].Equals(shadowsB[i]))
                            return false;
                    return true;
                }

                if (_offType != o._offType) return false;
                if (!FloatEquals(_offBorderRadius, o._offBorderRadius) ||
                    !FloatEquals(_offBorderWidth, o._offBorderWidth))
                    return false;
                if (!_offBorderColor.Equals(o._offBorderColor) || !_offBackgroundColor.Equals(o._offBackgroundColor))
                    return false;
                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is LampBakerKey o && Equals(o);
            }

            public override int GetHashCode()
            {
                var hc = new HashCode();
                hc.Add(_bakeWidth);
                hc.Add(_bakeHeight);
                hc.Add(_baseWidth);
                hc.Add(_baseHeight);
                hc.Add(_expansionLeft);
                hc.Add(_expansionRight);
                hc.Add(_expansionTop);
                hc.Add(_expansionBottom);
                hc.Add(_maxDynamicExpansion);
                hc.Add(_expandToFillGaps);
                hc.Add(_isOn);
                if (_isOn)
                {
                    hc.Add(_onStaticExtensionWidth);
                    hc.Add(_onStaticExtensionHeight);
                    hc.Add(_onBorderRadius);
                    hc.Add(_onOutlineWidth);
                    hc.Add(_onOutlineRadius);
                    hc.Add(_onOutlineColor);
                    hc.Add(_onBackgroundColor);
                    hc.Add(_onInnerShadowAlphaRatio);
                    for (var i = 0; i < _onDropShadows.Count; i++) AddDropShadow(hc, _onDropShadows[i]);
                }
                else
                {
                    hc.Add(_offType);
                    hc.Add(_offBorderRadius);
                    hc.Add(_offBorderWidth);
                    hc.Add(_offBorderColor);
                    hc.Add(_offBackgroundColor);
                }

                return hc.ToHashCode();
            }

            private static void AddDropShadow(HashCode hc, DropShadow s)
            {
                hc.Add(s.OffsetX);
                hc.Add(s.OffsetY);
                hc.Add(s.BlurX);
                hc.Add(s.BlurY);
                hc.Add(s.Color);
            }

            private static bool FloatEquals(float a, float b)
            {
                return Math.Abs(a - b) < Epsilons.FloatEpsilon;
            }
        }

        private readonly struct LampDrawerKey : IEquatable<LampDrawerKey>
        {
            private readonly IContentSnapshot _snapshot;
            private readonly float _bakeWidth, _bakeHeight;
            private readonly Color4 _color;
            private readonly float _offsetX, _offsetY;
            private readonly float _textWidth, _textHeight;

            public LampDrawerKey(IContentSnapshot snapshot, SizeF bakeSize, Color4 color,
                float offsetX, float offsetY, float textWidth, float textHeight)
            {
                _snapshot = snapshot;
                _bakeWidth = bakeSize.Width;
                _bakeHeight = bakeSize.Height;
                _color = color;
                _offsetX = offsetX;
                _offsetY = offsetY;
                _textWidth = textWidth;
                _textHeight = textHeight;
            }

            public bool Equals(LampDrawerKey o)
            {
                if (Math.Abs(_bakeWidth - o._bakeWidth) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_bakeHeight - o._bakeHeight) > Epsilons.FloatEpsilon) return false;
                if (!_color.Equals(o._color)) return false;
                if (Math.Abs(_offsetX - o._offsetX) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_offsetY - o._offsetY) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_textWidth - o._textWidth) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_textHeight - o._textHeight) > Epsilons.FloatEpsilon) return false;
                var a = _snapshot;
                var b = o._snapshot;
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                if (a.ContentHash != b.ContentHash) return false;
                return a.Equals(b);
            }

            public override bool Equals(object obj)
            {
                return obj is LampDrawerKey o && Equals(o);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    _snapshot?.ContentHash ?? 0, _bakeWidth, _bakeHeight, _color,
                    _offsetX, _offsetY, _textWidth, _textHeight);
            }
        }
    }
}