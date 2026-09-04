using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.ViewModels;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Buttons
{
    public class TIMSButtonStyle
    {
        public TIMSButtonStyle(
            Color4 idleBackgroundColor,
            Color4? idleDrawerColor,
            Color4? idleShadowReferenceColor,
            Color4 pressedBackgroundColor,
            Color4 pressedDrawerColor,
            Color4? pressedShadowReferenceColor,
            Color4 highlightedBackgroundColor,
            Color4 highlightedDrawerColor,
            Color4? highlightedShadowReferenceColor,
            int drawerPadding,
            bool showBorderWhenIdle
        )
        {
            IdleBackgroundColor.Value = idleBackgroundColor;
            IdleDrawerColor.Value = idleDrawerColor;
            IdleShadowReferenceColor.Value = idleShadowReferenceColor;
            PressedBackgroundColor.Value = pressedBackgroundColor;
            PressedDrawerColor.Value = pressedDrawerColor;
            PressedShadowReferenceColor.Value = pressedShadowReferenceColor;
            HighlightedBackgroundColor.Value = highlightedBackgroundColor;
            HighlightedDrawerColor.Value = highlightedDrawerColor;
            HighlightedShadowReferenceColor.Value = highlightedShadowReferenceColor;
            DrawerPadding.Value = drawerPadding;
            ShowBorderWhenIdle.Value = showBorderWhenIdle;
        }

        public Signal<Color4> IdleBackgroundColor { get; } = new Signal<Color4>();
        public Signal<Color4?> IdleDrawerColor { get; } = new Signal<Color4?>();
        public Signal<Color4?> IdleShadowReferenceColor { get; } = new Signal<Color4?>();
        public Signal<Color4> PressedBackgroundColor { get; } = new Signal<Color4>();
        public Signal<Color4?> PressedDrawerColor { get; } = new Signal<Color4?>();
        public Signal<Color4?> PressedShadowReferenceColor { get; } = new Signal<Color4?>();
        public Signal<Color4> HighlightedBackgroundColor { get; } = new Signal<Color4>();
        public Signal<Color4?> HighlightedDrawerColor { get; } = new Signal<Color4?>();
        public Signal<Color4?> HighlightedShadowReferenceColor { get; } = new Signal<Color4?>();
        public Signal<int> DrawerPadding { get; } = new Signal<int>();
        public Signal<bool> ShowBorderWhenIdle { get; } = new Signal<bool>();
    }

    public class TIMSButton : Widget<ButtonViewModel>, IButton
    {
        private static readonly Color4 AbsoluteHighlightColor = MonitorColors.White;
        private static readonly Color4 InnerGapShadowColor = "#23282D".ToColor4();
        private static readonly Color4 BorderColor = "#414651".ToColor4();
        private readonly PropertySlot<float> _baseHeight;
        private readonly PropertySlot<float> _baseWidth;
        private readonly IBoundsDrawer _boundsDrawer;
        private readonly Computed<bool> _currentlyHighlighted;
        private readonly Computed<bool> _currentlyPressed;
        private readonly Computed<ShadowPalette> _highlightedPalette;
        private readonly Computed<ShadowPalette> _idlePalette;
        private readonly PropertySlot<LayoutLength> _preferredHeightSlot;
        private readonly PropertySlot<LayoutLength> _preferredWidthSlot;
        private readonly Computed<ShadowPalette> _pressedPalette;
        private readonly ID2D1StrokeStyle _squareCapStroke;
        private readonly TIMSButtonStyle _style;

        public TIMSButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            TIMSButtonStyle style,
            IBoundsDrawer boundsDrawer = null,
            bool clickable = true,
            bool reboundImmediate = false,
            bool playPressAnimation = true
        ) : base(context, pos.X, pos.Y)
        {
            ViewModel = new ButtonViewModel(clickable, reboundImmediate, playPressAnimation);
            _style = style ?? throw new ArgumentNullException(nameof(style));
            _squareCapStroke = Context.D2D1Factory.CreateStrokeStyle(GeometryHelper.SquareStrokeStyleProperties);
            RegisterResource(_squareCapStroke);
            _boundsDrawer = boundsDrawer;
            if (_boundsDrawer != null) RegisterResource(_boundsDrawer);
            _preferredWidthSlot = CreatePropertySlot(DirtyType.Layout, width);
            _preferredHeightSlot = CreatePropertySlot(DirtyType.Layout, height);
            SkipArrangeWhenHidden = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenVisible = CreatePropertySlot(DirtyType.Layout, true);
            IncludeInTotalMajorDimensionSizeWhenHidden = CreatePropertySlot(DirtyType.Layout, false);
            _baseWidth = CreatePropertySlot(DirtyType.Visual, width.AbsoluteValue);
            _baseHeight = CreatePropertySlot(DirtyType.Visual, height.AbsoluteValue);
            PressedOverride = CreateRelayPropertySlot<bool>();
            _currentlyPressed = CreateComputed(() => PressedOverride || ViewModel.Pressed);
            Highlighted = CreatePropertySlot<bool>(DirtyType.Visual);
            _currentlyHighlighted = CreateComputed(() => !_currentlyPressed && Highlighted);
            _idlePalette = CreateComputed(() =>
                new ShadowPalette(style.IdleBackgroundColor.Value, style.IdleShadowReferenceColor.Value));
            _pressedPalette = CreateComputed(() =>
                new ShadowPalette(style.PressedBackgroundColor.Value, style.PressedShadowReferenceColor.Value));
            _highlightedPalette = CreateComputed(() =>
                new ShadowPalette(style.HighlightedBackgroundColor.Value, style.HighlightedShadowReferenceColor.Value));

            WatchEffect(
                style.ShowBorderWhenIdle,
                style.IdleBackgroundColor,
                style.IdleShadowReferenceColor,
                style.PressedBackgroundColor,
                style.PressedShadowReferenceColor,
                style.HighlightedBackgroundColor,
                style.HighlightedShadowReferenceColor,
                style.IdleDrawerColor,
                style.PressedDrawerColor,
                style.HighlightedDrawerColor,
                style.DrawerPadding,
                _currentlyPressed,
                _currentlyHighlighted
            );

            if (boundsDrawer is ITrackable trackable) WatchEffect(trackable);
        }

        public PropertySlot<bool> Clickable => ViewModel.Clickable;
        public PropertySlot<bool> Highlighted { get; }
        public PropertySlot<bool> PressedOverride { get; }
        public PropertySlot<bool> SkipArrangeWhenHidden { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenVisible { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenHidden { get; }

        public RectangleF BaseBounds => new RectangleF(0, 0, _baseWidth, _baseHeight);
        public override RectangleF SelfRelativeDirtyBounds => BaseBounds;
        protected override IList<RectangleF> LocalClickBoundsList => new[] { BaseBounds };

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

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

        public float MarginWidth => 0;
        public float MarginHeight => 0;
        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangeWhenHidden;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenVisible => IncludeInTotalMajorDimensionSizeWhenVisible;
        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden => IncludeInTotalMajorDimensionSizeWhenHidden;

        public void SetLayoutSize(float width, float height)
        {
            _baseWidth.Value = width;
            _baseHeight.Value = height;
        }

        public event Action OnClick
        {
            add => ViewModel.OnClick += value;
            remove => ViewModel.OnClick -= value;
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            DrawBackgroundWithCache(false, false);
            DrawBackgroundWithCache(true, false);
            DrawBackgroundWithCache(false, true);
            DrawContent(_style.IdleDrawerColor.Value);
        }

        protected override void OnDraw(float totalScale)
        {
            if (_currentlyPressed)
            {
                DrawBackgroundWithCache(true, false);
                DrawContent(_style.PressedDrawerColor.Value);
            }
            else if (_currentlyHighlighted)
            {
                DrawBackgroundWithCache(false, true);
                DrawContent(_style.HighlightedDrawerColor.Value);
            }
            else
            {
                DrawBackgroundWithCache(false, false);
                DrawContent(_style.IdleDrawerColor.Value);
            }
        }

        private void DrawBackgroundWithCache(bool isPressed, bool isHighlighted)
        {
            var w = _baseWidth.Value;
            var h = _baseHeight.Value;

            Color4 bgColor;
            Color4? shadowRef;

            if (isPressed)
            {
                bgColor = _style.PressedBackgroundColor.Value;
                shadowRef = _style.PressedShadowReferenceColor.Value;
            }
            else if (isHighlighted)
            {
                bgColor = _style.HighlightedBackgroundColor.Value;
                shadowRef = _style.HighlightedShadowReferenceColor.Value;
            }
            else
            {
                bgColor = _style.IdleBackgroundColor.Value;
                shadowRef = _style.IdleShadowReferenceColor.Value;
            }

            var key = new TIMSButtonBakerKey(
                w,
                h,
                isPressed,
                isHighlighted,
                bgColor,
                shadowRef,
                _style.IdleBackgroundColor.Value,
                _style.IdleShadowReferenceColor.Value,
                _style.ShowBorderWhenIdle.Value
            );

            var baker = Context.GetBakerCache().GetOrCreateBaker<TIMSButton, TIMSButtonBakerKey>(
                Context,
                key,
                BakerPrescaleMode.AutoCubic
            );

            baker.BakeAndDraw(SelfRelativeDirtyBounds, () => DrawBackground(w, h, isPressed, isHighlighted));
        }

        private void DrawContent(Color4? color)
        {
            if (_boundsDrawer == null) return;
            var w = _baseWidth.Value;
            var h = _baseHeight.Value;
            RectangleF bounds = _currentlyPressed
                ? new RawRectF(5, 5, w - 6, h - 5)
                : new RawRectF(5, 4, w - 6, h - 6);
            bounds.Inflate(-_style.DrawerPadding, -_style.DrawerPadding);
            if (_currentlyPressed) bounds.Offset(2, 2);

            _boundsDrawer.Draw(bounds, color ?? Colors.Transparent);
        }

        private void DrawBackground(float w, float h, bool isPressed, bool isHighlighted)
        {
            var dc = Context.DeviceContext;
            var oldAntialiasMode = dc.AntialiasMode;
            dc.AntialiasMode = AntialiasMode.Aliased;
            var currentPalette = (isPressed ? _pressedPalette : isHighlighted ? _highlightedPalette : _idlePalette)
                .Value;
            var idlePalette = _idlePalette.Value;
            if (isPressed)
            {
                DrawPolyLine(dc, AbsoluteHighlightColor, w - 1, 7, w - 1, h - 4, w - 2, h - 3, w - 2, h - 6, w - 3,
                    h - 6,
                    w - 3, h - 2, w - 4, h - 1, w - 4, h - 6, w - 5, h - 5, w - 5, h - 1, w - 6, h - 1, w - 6, h - 4);
                DrawPolyLine(dc, AbsoluteHighlightColor, 5, h - 1, w - 7, h - 1);

                DrawPolyLine(dc, currentPalette.Light1, w - 1, 5, w - 1, 6, w - 2, 6, w - 2, h - 7, w - 3, h - 7,
                    w - 3, 7, w - 4,
                    8, w - 4, h - 7, w - 7, h - 4, w - 7, h - 3, 6, h - 3);
                DrawPolyLine(dc, currentPalette.Light1, 4, h - 1, 5, h - 2, w - 7, h - 2);

                DrawPolyLine(dc, currentPalette.Light2, w - 1, 4, w - 2, 3, w - 2, 5, w - 3, 4, w - 3, 6, w - 4, 5,
                    w - 4, 7,
                    w - 5, 6, w - 5, h - 7);
                DrawPolyLine(dc, currentPalette.Light2, 4, h - 2, 6, h - 4, w - 8, h - 4);

                DrawPolyLine(dc, currentPalette.Background, w - 5, 5, w - 6, 5, w - 6, h - 6, w - 7, h - 5, 7,
                    h - 5);

                Context.CommonBrush.Color = currentPalette.Background;
                dc.FillRectangle(new RectangleF(7, 5, w - 13, h - 9), Context.CommonBrush);

                DrawPolyLine(dc, currentPalette.Dark1, w - 3, 3, w - 4, 4);
                DrawPolyLine(dc, currentPalette.Dark1, 3, h - 2, 5, h - 4, 5, 6, 6, 5, 6, h - 5);

                DrawPolyLine(dc, currentPalette.Dark3, w - 3, 2, w - 4, 3, w - 5, 3, w - 5, 4, 6, 4);
                DrawPolyLine(dc, currentPalette.Dark3, 3, h - 3, 4, h - 4);

                DrawPolyLine(dc, InnerGapShadowColor, w - 4, 1, 4, 1, 3, 2, w - 4, 2);
                DrawPolyLine(dc, InnerGapShadowColor, 1, h - 4, 1, 4, 2, 3, 2, h - 3, 3, h - 4, 3, 3, 4, 3, 4,
                    h - 5);
                DrawPolyLine(dc, InnerGapShadowColor, 5, 5, 5, 3, w - 6, 3);

                DrawPolyLine(dc, BorderColor, w - 1, 3, w - 4, 0, 4, 0, 0, 4, 0, h - 4, 3, h - 1);
            }
            else
            {
                var topLeftSpecularColor = isHighlighted ? currentPalette.Background : AbsoluteHighlightColor;
                var topLeftInnerColor1 = isHighlighted ? currentPalette.Background : currentPalette.Light1;
                var topLeftInnerColor2 = isHighlighted ? currentPalette.Background : currentPalette.Light2;
                var topLeftInnerColor3 = isHighlighted ? currentPalette.Background : currentPalette.Light3;
                var surfaceTransitColor = currentPalette.Background;
                var bottomRightColor1 = isHighlighted ? idlePalette.Light2 : currentPalette.Dark1;
                var bottomRightColor2 = isHighlighted ? idlePalette.Light2 : currentPalette.Dark2;
                var bottomRightCoreColor = isHighlighted ? idlePalette.Dark3 : currentPalette.Dark3;
                DrawPolyLine(dc, topLeftSpecularColor, 5, 0, 3, 0, 0, 3, 0, 5);
                DrawPolyLine(dc, topLeftSpecularColor, 1, 3, 3, 1, 3, 3, 2, 3);

                DrawPolyLine(dc, topLeftInnerColor1, w - 7, 0, 6, 0, 5, 1, 5, 3, 4, 4, 4, 1);
                DrawPolyLine(dc, topLeftInnerColor1, 0, h - 8, 0, 6, 1, 5, 3, 5, 3, 4, 1, 4);

                DrawPolyLine(dc, topLeftInnerColor2, w - 6, 0, w - 7, 1, 6, 1, 6, 3, 7, 2, w - 8, 2);
                DrawPolyLine(dc, topLeftInnerColor2, 0, h - 6, 0, h - 7, 1, h - 7, 1, 6, 2, 6, 2, h - 8, 3, h - 9, 3,
                    6,
                    5, 4);

                DrawPolyLine(dc, topLeftInnerColor3, w - 5, 0, w - 8, 3, 6, 3);
                DrawPolyLine(dc, topLeftInnerColor3, 0, h - 5, 1, h - 4, 2, h - 5, 1, h - 5, 1, h - 6, 3, h - 6, 2,
                    h - 7, 4, h - 7, 3, h - 8, 4, h - 8, 4, 6);

                DrawPolyLine(dc, surfaceTransitColor, w - 5, 1, w - 8, 4, 6, 4, 5, 5, 5, h - 6, 4, h - 6, 2, h - 4);

                Context.CommonBrush.Color = currentPalette.Background;
                dc.FillRectangle(new RectangleF(6, 5, w - 13, h - 10), Context.CommonBrush);

                DrawPolyLine(dc, bottomRightColor1, w - 4, 1, w - 6, 3, w - 6, h - 7, w - 7, h - 6, w - 7, 4);

                DrawPolyLine(dc, bottomRightColor2, w - 4, 2, w - 5, 3, w - 5, h - 7);
                DrawPolyLine(dc, bottomRightColor2, 3, h - 4, 4, h - 5, 4, h - 5, w - 7, h - 5, w - 6, h - 6);

                DrawPolyLine(dc, bottomRightCoreColor, 3, h - 2, 2, h - 3);
                DrawPolyLine(dc, bottomRightCoreColor, w - 2, 3, w - 2, h - 5, w - 3, h - 4, w - 3, 2, w - 4, 3,
                    w - 4,
                    h - 3, w - 5, h - 2, w - 5, h - 6, w - 6, h - 5, w - 6, h - 2, 4, h - 2, 3, h - 3, w - 7, h - 3,
                    w - 7, h - 4, 4, h - 4);

                DrawPolyLine(dc, _style.ShowBorderWhenIdle ? BorderColor : bottomRightCoreColor, w - 4, 0, w - 1, 3,
                    w - 1, h - 5, w - 5, h - 1, 3, h - 1, 0, h - 4);
            }

            dc.AntialiasMode = oldAntialiasMode;
        }

        private void DrawPolyLine(ID2D1DeviceContext dc, Color4 color, params float[] vertices)
        {
            Context.CommonBrush.Color = color;
            dc.DrawAliasedPolyLine(Context.CommonBrush, _squareCapStroke, vertices);
        }

        protected override bool OnPointerDown(Vector2 localPoint)
        {
            ViewModel.TryClick();
            return true;
        }

        private readonly struct TIMSButtonBakerKey : IEquatable<TIMSButtonBakerKey>
        {
            public readonly float Width;
            public readonly float Height;
            public readonly bool IsPressed;
            public readonly bool IsHighlighted;
            public readonly Color4 BackgroundColor;
            public readonly Color4? ShadowReferenceColor;
            public readonly Color4 IdleBackgroundColor;
            public readonly Color4? IdleShadowReferenceColor;
            public readonly bool ShowBorderWhenIdle;

            public TIMSButtonBakerKey(
                float width,
                float height,
                bool isPressed,
                bool isHighlighted,
                Color4 backgroundColor,
                Color4? shadowReferenceColor,
                Color4 idleBackgroundColor,
                Color4? idleShadowReferenceColor,
                bool showBorderWhenIdle)
            {
                Width = width;
                Height = height;
                IsPressed = isPressed;
                IsHighlighted = isHighlighted;
                BackgroundColor = backgroundColor;
                ShadowReferenceColor = shadowReferenceColor;
                IdleBackgroundColor = idleBackgroundColor;
                IdleShadowReferenceColor = idleShadowReferenceColor;
                ShowBorderWhenIdle = showBorderWhenIdle;
            }

            public bool Equals(TIMSButtonBakerKey other)
            {
                return Math.Abs(Width - other.Width) < Epsilons.FloatEpsilon &&
                       Math.Abs(Height - other.Height) < Epsilons.FloatEpsilon &&
                       IsPressed == other.IsPressed &&
                       IsHighlighted == other.IsHighlighted &&
                       BackgroundColor.Equals(other.BackgroundColor) &&
                       Nullable.Equals(ShadowReferenceColor, other.ShadowReferenceColor) &&
                       IdleBackgroundColor.Equals(other.IdleBackgroundColor) &&
                       Nullable.Equals(IdleShadowReferenceColor, other.IdleShadowReferenceColor) &&
                       ShowBorderWhenIdle == other.ShowBorderWhenIdle;
            }

            public override bool Equals(object obj)
            {
                return obj is TIMSButtonBakerKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Width);
                hashCode.Add(Height);
                hashCode.Add(IsPressed);
                hashCode.Add(IsHighlighted);
                hashCode.Add(BackgroundColor);
                hashCode.Add(ShadowReferenceColor);
                hashCode.Add(IdleBackgroundColor);
                hashCode.Add(IdleShadowReferenceColor);
                hashCode.Add(ShowBorderWhenIdle);
                return hashCode.ToHashCode();
            }
        }

        private struct ShadowPalette
        {
            public readonly Color4 Background;
            public readonly Color4 Light1;
            public readonly Color4 Light2;
            public readonly Color4 Light3;
            public readonly Color4 Dark1;
            public readonly Color4 Dark2;
            public readonly Color4 Dark3;

            public ShadowPalette(Color4 backgroundColor, Color4? shadowRef)
            {
                var refColor = shadowRef ?? backgroundColor;
                Background = backgroundColor;
                Light1 = Color4.Lerp(backgroundColor, MonitorColors.White, 0.85f);
                Light2 = Color4.Lerp(backgroundColor, MonitorColors.White, 0.6f);
                Light3 = Color4.Lerp(backgroundColor, MonitorColors.White, 0.4f);
                Dark1 = Color4.Lerp(refColor, Colors.Black, 0.15f);
                Dark2 = Color4.Lerp(refColor, Colors.Black, 0.4f);
                Dark3 = Color4.Lerp(refColor, Colors.Black, 0.65f);
            }
        }
    }
}