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
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.ViewModels;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Buttons
{
    public struct VectorButtonInnerShadow : IEquatable<VectorButtonInnerShadow>
    {
        public float Width { get; }
        public ImageInnerShadow Image { get; }

        public VectorButtonInnerShadow(float width, ImageInnerShadow image)
        {
            Width = width;
            Image = image;
        }

        public bool Equals(VectorButtonInnerShadow other)
        {
            return Math.Abs(Width - other.Width) < Epsilons.FloatEpsilon && Image.Equals(other.Image);
        }

        public override bool Equals(object obj)
        {
            return obj is VectorButtonInnerShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Image);
        }

        public static bool operator ==(VectorButtonInnerShadow left, VectorButtonInnerShadow right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VectorButtonInnerShadow left, VectorButtonInnerShadow right)
        {
            return !left.Equals(right);
        }
    }

    public class VectorButtonStyle : IDisposable
    {
        public VectorButtonStyle(
            float borderRadius,
            float drawerPadding,
            Color4 idleBackgroundColor,
            Color4? idleDrawerColor,
            Color4 pressedBackgroundColor,
            Color4 pressedDrawerColor,
            DropShadow[] idleDropShadows = null,
            VectorButtonInnerShadow? idleTopLeftInnerShadow = null,
            VectorButtonInnerShadow? idleBottomRightInnerShadow = null,
            VectorButtonInnerShadow? pressedTopLeftInnerShadow = null,
            VectorButtonInnerShadow? pressedBottomRightInnerShadow = null
        )
        {
            BorderRadius.Value = borderRadius;
            DrawerPadding.Value = drawerPadding;
            IdleBackgroundColor.Value = idleBackgroundColor;
            IdleDrawerColor.Value = idleDrawerColor;
            PressedBackgroundColor.Value = pressedBackgroundColor;
            PressedDrawerColor.Value = pressedDrawerColor;
            IdleDropShadows = new ReactiveList<DropShadow>(idleDropShadows);
            IdleTopLeftInnerShadow.Value = idleTopLeftInnerShadow;
            IdleBottomRightInnerShadow.Value = idleBottomRightInnerShadow;
            PressedTopLeftInnerShadow.Value = pressedTopLeftInnerShadow;
            PressedBottomRightInnerShadow.Value = pressedBottomRightInnerShadow;
        }

        public Signal<float> BorderRadius { get; } = new Signal<float>();
        public Signal<float> DrawerPadding { get; } = new Signal<float>();
        public Signal<Color4> IdleBackgroundColor { get; } = new Signal<Color4>();
        public Signal<Color4?> IdleDrawerColor { get; } = new Signal<Color4?>();
        public Signal<Color4> PressedBackgroundColor { get; } = new Signal<Color4>();
        public Signal<Color4> PressedDrawerColor { get; } = new Signal<Color4>();
        public ReactiveList<DropShadow> IdleDropShadows { get; }

        public Signal<VectorButtonInnerShadow?> IdleTopLeftInnerShadow { get; } =
            new Signal<VectorButtonInnerShadow?>();

        public Signal<VectorButtonInnerShadow?> IdleBottomRightInnerShadow { get; } =
            new Signal<VectorButtonInnerShadow?>();

        public Signal<VectorButtonInnerShadow?> PressedTopLeftInnerShadow { get; } =
            new Signal<VectorButtonInnerShadow?>();

        public Signal<VectorButtonInnerShadow?> PressedBottomRightInnerShadow { get; } =
            new Signal<VectorButtonInnerShadow?>();

        public void Dispose()
        {
            IdleDropShadows.Dispose();
        }
    }

    public class VectorButton : Widget<ButtonViewModel>, IButton
    {
        private readonly PropertySlot<float> _baseHeight;
        private readonly PropertySlot<float> _baseWidth;
        private readonly IBoundsDrawer _boundsDrawer;
        private readonly Computed<bool> _currentlyPressed;
        private readonly Baker _idleBackgroundBaker;
        private readonly ImageInnerShadowEffect _idleBottomRightEffect;
        private readonly CommandRecorder _idleBottomRightRecorder;
        private readonly InnerShadowEffectChain _idleInnerShadowChain;
        private readonly ImageInnerShadowEffect _idleTopLeftEffect;
        private readonly CommandRecorder _idleTopLeftRecorder;
        private readonly PropertySlot<LayoutLength> _preferredHeightSlot;
        private readonly PropertySlot<LayoutLength> _preferredWidthSlot;
        private readonly Baker _pressedBackgroundBaker;
        private readonly ImageInnerShadowEffect _pressedBottomRightEffect;
        private readonly CommandRecorder _pressedBottomRightRecorder;
        private readonly InnerShadowEffectChain _pressedInnerShadowChain;
        private readonly ImageInnerShadowEffect _pressedTopLeftEffect;
        private readonly CommandRecorder _pressedTopLeftRecorder;
        private readonly VectorButtonStyle _style;

        public VectorButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            VectorButtonStyle style,
            IBoundsDrawer boundsDrawer = null,
            bool clickable = true,
            bool reboundImmediate = false,
            bool playPressAnimation = true
        ) : base(context, pos.X, pos.Y)
        {
            ViewModel = new ButtonViewModel(clickable, reboundImmediate, playPressAnimation);
            _style = style;
            _idleBackgroundBaker = new Baker(Context);
            RegisterResource(_idleBackgroundBaker);
            _pressedBackgroundBaker = new Baker(Context);
            RegisterResource(_pressedBackgroundBaker);
            _boundsDrawer = boundsDrawer;
            if (_boundsDrawer != null) RegisterResource(_boundsDrawer);
            _preferredWidthSlot = CreatePropertySlot(DirtyType.Layout, width);
            _preferredHeightSlot = CreatePropertySlot(DirtyType.Layout, height);
            _baseWidth = CreatePropertySlot(DirtyType.Visual, width.AbsoluteValue);
            _baseHeight = CreatePropertySlot(DirtyType.Visual, height.AbsoluteValue);
            PressedOverride = CreateRelayPropertySlot<bool>();
            _currentlyPressed = CreateComputed(() => PressedOverride || ViewModel.Pressed);
            _idleInnerShadowChain = new InnerShadowEffectChain(context.DeviceContext);
            RegisterResource(_idleInnerShadowChain);
            _idleTopLeftEffect = _idleInnerShadowChain.AddImageShadow();
            _idleTopLeftRecorder = new CommandRecorder(context);
            RegisterResource(_idleTopLeftRecorder);
            _idleBottomRightEffect = _idleInnerShadowChain.AddImageShadow();
            _idleBottomRightRecorder = new CommandRecorder(context);
            RegisterResource(_idleBottomRightRecorder);
            _pressedInnerShadowChain = new InnerShadowEffectChain(context.DeviceContext);
            RegisterResource(_pressedInnerShadowChain);
            _pressedTopLeftEffect = _pressedInnerShadowChain.AddImageShadow();
            _pressedTopLeftRecorder = new CommandRecorder(context);
            RegisterResource(_pressedTopLeftRecorder);
            _pressedBottomRightEffect = _pressedInnerShadowChain.AddImageShadow();
            _pressedBottomRightRecorder = new CommandRecorder(context);
            RegisterResource(_pressedBottomRightRecorder);
            WatchEffect(() =>
                {
                    _idleBackgroundBaker.Refresh();
                    _pressedBackgroundBaker.Refresh();
                    _idleTopLeftRecorder.Invalidate();
                    _idleBottomRightRecorder.Invalidate();
                    _pressedTopLeftRecorder.Invalidate();
                    _pressedBottomRightRecorder.Invalidate();
                }, _baseWidth, _baseHeight, _style.BorderRadius,
                _style.IdleTopLeftInnerShadow, _style.IdleBottomRightInnerShadow,
                _style.PressedTopLeftInnerShadow, _style.PressedBottomRightInnerShadow);
            WatchEffect(() => { _idleBackgroundBaker.Refresh(); }, _style.IdleBackgroundColor, _style.IdleDropShadows);
            WatchEffect(() => { _pressedBackgroundBaker.Refresh(); }, _style.PressedBackgroundColor);
            WatchEffect(_currentlyPressed, _style.DrawerPadding, _style.IdleDrawerColor, _style.PressedDrawerColor);
            WatchEffect(boundsDrawer);
        }

        public PropertySlot<bool> Clickable => ViewModel.Clickable;
        public PropertySlot<bool> PressedOverride { get; }
        public RectangleF BaseBounds => new RectangleF(0, 0, _baseWidth, _baseHeight);

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var rect = BaseBounds;
                float maxSpread = 0;
                var dropShadows = _style.IdleDropShadows;
                if (dropShadows != null && dropShadows.Count > 0)
                    maxSpread = dropShadows.Max(shadow => shadow.MaxSpread);

                rect.Inflate(maxSpread, maxSpread);
                return rect;
            }
        }

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
        public bool SkipArrangeWhenHidden => true;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;

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

        private void RecordTopLeftMaskGeometry(VectorButtonInnerShadow shadow)
        {
            using (var geometry = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = geometry.Open())
                {
                    sink.AddTopLeftInnerBevelFigure(_baseWidth, _baseHeight, shadow.Width, _style.BorderRadius,
                        shadow.Image.Blur * 3);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillGeometry(geometry, Context.CommonBrush);
            }
        }

        private void RecordBottomRightMaskGeometry(VectorButtonInnerShadow shadow)
        {
            using (var geometry = Context.D2D1Factory.CreatePathGeometry())
            {
                using (var sink = geometry.Open())
                {
                    sink.AddBottomRightInnerBevelFigure(_baseWidth, _baseHeight, shadow.Width, _style.BorderRadius,
                        shadow.Image.Blur * 3);
                    sink.Close();
                }

                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillGeometry(geometry, Context.CommonBrush);
            }
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            _idleBackgroundBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(false));
            _pressedBackgroundBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(true));
            DrawContent(_style.IdleDrawerColor.Value ?? Colors.Transparent);
        }

        protected override void OnDraw(float totalScale)
        {
            if (_currentlyPressed.Value)
            {
                _pressedBackgroundBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(true));
                DrawContent(_style.PressedDrawerColor.Value);
            }
            else
            {
                _idleBackgroundBaker.BakeAndDraw(SelfRelativeDirtyBounds, () => Draw(false));
                DrawContent(_style.IdleDrawerColor.Value ?? Colors.Transparent);
            }
        }

        private void DrawContent(Color4 color)
        {
            if (_boundsDrawer == null) return;
            var bounds = BaseBounds;
            var padding = _style.DrawerPadding.Value;
            bounds.Inflate(-padding, -padding);
            if (_currentlyPressed.Value)
            {
                bounds.X += 2;
                bounds.Y += 2;
            }

            _boundsDrawer.Draw(bounds, color);
        }

        private void Draw(bool pressed)
        {
            var borderRadius = _style.BorderRadius.Value;
            var rect = new RoundedRectangle(new RectangleF(0, 0, _baseWidth, _baseHeight), borderRadius, borderRadius);

            if (!pressed)
                Context.DropShadowProcessor.DrawWithDropShadows(_style.IdleDropShadows, shadowAction: () =>
                {
                    Context.CommonBrush.Color = Colors.Black;
                    Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush);
                });

            if (pressed)
            {
                var topLeft = _style.PressedTopLeftInnerShadow.Value ?? _style.IdleTopLeftInnerShadow.Value;
                var bottomRight = _style.PressedBottomRightInnerShadow.Value ?? _style.IdleBottomRightInnerShadow.Value;

                UpdateShadowEffect(_pressedTopLeftEffect, _pressedTopLeftRecorder, topLeft, RecordTopLeftMaskGeometry);
                UpdateShadowEffect(_pressedBottomRightEffect, _pressedBottomRightRecorder, bottomRight,
                    RecordBottomRightMaskGeometry);

                Context.CommonBrush.Color = _style.PressedBackgroundColor.Value;
                Context.InnerShadowProcessor.DrawWithInnerShadows(_pressedInnerShadowChain,
                    () => { Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush); });
            }
            else
            {
                var topLeft = _style.IdleTopLeftInnerShadow.Value;
                var bottomRight = _style.IdleBottomRightInnerShadow.Value;
                UpdateShadowEffect(_idleTopLeftEffect, _idleTopLeftRecorder, topLeft, RecordTopLeftMaskGeometry);
                UpdateShadowEffect(_idleBottomRightEffect, _idleBottomRightRecorder, bottomRight,
                    RecordBottomRightMaskGeometry);
                Context.CommonBrush.Color = _style.IdleBackgroundColor.Value;
                Context.InnerShadowProcessor.DrawWithInnerShadows(_idleInnerShadowChain,
                    () => { Context.DeviceContext.FillRoundedRectangle(rect, Context.CommonBrush); });
            }
        }

        private static void UpdateShadowEffect(
            ImageInnerShadowEffect effect,
            CommandRecorder recorder,
            VectorButtonInnerShadow? shadowSetting,
            Action<VectorButtonInnerShadow> recordAction
        )
        {
            if (shadowSetting.HasValue)
            {
                effect.IsEnabled = true;
                effect.Update(shadowSetting.Value.Image,
                    recorder.RecordTransformed(() => recordAction(shadowSetting.Value)));
            }
            else
            {
                effect.IsEnabled = false;
            }
        }

        protected override bool OnPointerDown(Vector2 localPoint)
        {
            ViewModel.TryClick();
            return true;
        }
    }
}