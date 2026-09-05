using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.DirtyUpdate;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using Vortice.Mathematics;

namespace JREMonitors.Core.Widgets
{
    public enum DirtyType
    {
        Visual,
        Layout
    }

    public enum EffectPhase
    {
        State,
        Visual,
        Commit
    }

    public struct UpdateOptions
    {
        public bool UpdateViewModel { get; private set; }

        public bool IgnoreHidden { get; private set; }

        public static readonly UpdateOptions Default = new UpdateOptions
            { UpdateViewModel = true, IgnoreHidden = false };

        public static readonly UpdateOptions WarmUp = new UpdateOptions
            { UpdateViewModel = false, IgnoreHidden = true };

        public static readonly UpdateOptions FirstOffScreenRender = new UpdateOptions
            { UpdateViewModel = true, IgnoreHidden = true };
    }

    /// <summary>
    ///     响应式 UI 组件基类,屏上元素的共同骨架，负责"状态 → 脏区 → 绘制"的帧驱动流水线与子树管理。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         每个 Widget 暴露 <see cref="X" />/<see cref="Y" />/<see cref="Scale" />/<see cref="IsVisible" />
    ///         等响应式属性；Monitor 每帧调用 <c>Update</c> 驱动五步流水线——ViewModel 汇入数据 →
    ///         <see cref="OnArrangeLayout" /> 布局子项 → 由脏标记门控的 <see cref="OnDraw" /> 绘制与资源更新
    ///         → 沿树汇总脏区上报 <see cref="DirtyUpdateManager" />。只有变化才重绘，隐藏节点沿途短路。
    ///     </para>
    ///     <para>
    ///         <c>WatchEffect</c> 以响应式效果挂接数据源，依赖变化自动置脏；子树经
    ///         <see cref="AddChild" /> 挂接并订阅其失效以汇总子脏标记；点击经
    ///         <see cref="HandlePointerDown" /> 逆序派发（顶层优先）；<see cref="RequestBlock" /> 与
    ///         <see cref="IsTypeBlocked" /> 借由 BlockingService 实现阻塞；整树以
    ///         <see cref="Dispose" />/<see cref="Reset" /> 统一回收与复位。
    ///     </para>
    /// </remarks>
    public abstract class Widget : IDisposable
    {
        private readonly List<ReactiveEffect> _commitEffects = new List<ReactiveEffect>();
        private readonly DisposableStack _disposableStack = new DisposableStack();
        private readonly List<WidgetBlockingEntry> _resolvedBlocks = new List<WidgetBlockingEntry>();
        private readonly List<IResourceSlot> _resourceSlots = new List<IResourceSlot>();
        private readonly List<ReactiveEffect> _stateEffects = new List<ReactiveEffect>();
        private readonly List<ReactiveEffect> _visualEffects = new List<ReactiveEffect>();
        private BlockingService _blockingService;
        private RectangleF _cachedDeepBounds;
        private DataHub _hub;
        private bool _isDeepBoundsDirty = true;
        private bool _reseted = true;
        private ViewModel _viewModel;

        protected Widget(RenderContext context, float x = 0, float y = 0, float scale = 1, bool isVisible = true)
        {
            Context = context;
            X = CreatePropertySlot(DirtyType.Visual, x);
            Y = CreatePropertySlot(DirtyType.Visual, y);
            Scale = CreatePropertySlot(DirtyType.Visual, scale);
            IsVisible = CreatePropertySlot(DirtyType.Layout, isVisible);
        }

        public RenderContext Context { get; }
        public PropertySlot<float> X { get; }
        public PropertySlot<float> Y { get; }
        public PropertySlot<float> Scale { get; }
        public PropertySlot<bool> IsVisible { get; }

        public Vector2 Pos => new Vector2(X, Y);
        public bool IsGlobalPosition { get; private set; }
        public bool IsLayoutDirty { get; private set; }

        public bool IsVisualDirty => IsVisibleDirty || IsLayoutDirty;
        public bool IsChildrenLayoutDirty { get; private set; }
        public bool IsChildrenVisualDirty { get; private set; }

        protected virtual bool SkipUpdateWhenHidden { get; set; } = true;
        public bool IsFirstUpdate { get; private set; } = true;
        public bool IsGlobalFirstUpdate { get; private set; } = true;
        protected bool IsFirstRender { get; private set; } = true;
        protected bool IsOffScreen { get; private set; }

        public List<Widget> Children { get; } = new List<Widget>();

        public abstract RectangleF SelfRelativeDirtyBounds { get; }
        protected virtual bool MergeChildrenDirtyBounds { get; set; }
        protected virtual IList<RectangleF> LocalClickBoundsList => Array.Empty<RectangleF>();
        protected RectangleF LastGlobalDirtyBounds { get; private set; }
        public virtual float? RefreshSpeed { get; set; }
        public bool IsVisibleDirty { get; private set; }
        protected bool ManualIsDirtyMark { get; private set; }
        public virtual bool IsUpdateBlocked => false;
        public virtual bool IsPointerDownBlocked => false;

        protected ViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                    throw new InvalidOperationException($"Widget {GetType().Name} already has a ViewModel.");
                _viewModel = value;
            }
        }

        public bool IsDirty => IsVisualDirty;

        public void Dispose()
        {
            for (var i = 0; i < Children.Count; i++)
            {
                Children[i].OnInvalidated -= OnChildInvalidated;
                Children[i].Dispose();
            }

            Children.Clear();
            _viewModel?.Dispose();
            OnDispose();
            _disposableStack.Dispose();
        }


        private event Action<DirtyType> OnInvalidated;

        protected void Invalidate(DirtyType type)
        {
            if (type == DirtyType.Layout) IsLayoutDirty = true;
            IsVisibleDirty = true;
            _isDeepBoundsDirty = true;
            OnInvalidated?.Invoke(type);
        }

        private void OnChildInvalidated(DirtyType type)
        {
            if (type == DirtyType.Layout) IsChildrenLayoutDirty = true;
            IsChildrenVisualDirty = true;
            _isDeepBoundsDirty = true;
            OnInvalidated?.Invoke(type);
        }


        protected Computed<T> CreateComputed<T>(Func<T> supplier, bool enableDynamicUnbinding = true)
        {
            var computed = new Computed<T>(supplier, enableDynamicUnbinding);
            RegisterResource(computed);
            return computed;
        }

        protected PropertySlot<T> CreateRelayPropertySlot<T>(T initial = default)
        {
            var slot = new PropertySlot<T>(initial);
            RegisterResource(slot);
            return slot;
        }

        protected PropertySlot<T> CreateRelayPropertySlot<T>(IValueSignal<T> source)
        {
            var slot = new PropertySlot<T>(source);
            RegisterResource(slot);
            return slot;
        }

        protected PropertySlot<T> CreatePropertySlot<T>(
            DirtyType dirtyType,
            T initial = default
        )
        {
            var slot = CreateRelayPropertySlot(initial);
            slot.OnInvalidated += () => Invalidate(dirtyType);
            return slot;
        }

        protected PropertySlot<T> CreatePropertySlot<T>(DirtyType dirtyType, IValueSignal<T> source)
        {
            var slot = CreateRelayPropertySlot(source);
            slot.OnInvalidated += () => Invalidate(dirtyType);
            return slot;
        }

        protected ReactiveList<T> CreateRelayReactiveList<T>(IEnumerable<T> initial = null, int capacity = 0)
        {
            var list = new ReactiveList<T>(initial, capacity);
            RegisterResource(list);
            return list;
        }

        protected ReactiveList<T> CreateRelayReactiveList<T>(IValueSignal<IReadOnlyList<T>> source)
        {
            var list = new ReactiveList<T>(source);
            RegisterResource(list);
            return list;
        }

        protected ReactiveList<T> CreateReactiveList<T>(DirtyType dirtyType, IEnumerable<T> initial = null,
            int capacity = 0)
        {
            var list = CreateRelayReactiveList(initial, capacity);
            list.OnInvalidated += () => Invalidate(dirtyType);
            return list;
        }

        protected ReactiveList<T> CreateReactiveList<T>(DirtyType dirtyType, IValueSignal<IReadOnlyList<T>> source)
        {
            var list = CreateRelayReactiveList(source);
            list.OnInvalidated += () => Invalidate(dirtyType);
            return list;
        }

        protected ReactiveArray<T> CreateRelayReactiveArray<T>(int length)
        {
            var array = new ReactiveArray<T>(length);
            RegisterResource(array);
            return array;
        }

        protected ReactiveArray<T> CreateRelayReactiveArray<T>(int length, IValueSignal<IReadOnlyList<T>> source)
        {
            var array = new ReactiveArray<T>(length, source);
            RegisterResource(array);
            return array;
        }

        protected ReactiveArray<T> CreateReactiveArray<T>(DirtyType dirtyType, int length)
        {
            var array = CreateRelayReactiveArray<T>(length);
            array.OnInvalidated += () => Invalidate(dirtyType);
            return array;
        }

        protected ReactiveArray<T> CreateReactiveArray<T>(DirtyType dirtyType, int length,
            IValueSignal<IReadOnlyList<T>> source)
        {
            var array = CreateRelayReactiveArray(length, source);
            array.OnInvalidated += () => Invalidate(dirtyType);
            return array;
        }


        protected void WatchEffect(Action action, params ITrackable[] trackables)
        {
            WatchEffect(EffectPhase.Commit, () =>
            {
                ReactiveScope.TrackAll(trackables);
                action();
            }, false);
        }

        protected void WatchEffect(params ITrackable[] trackables)
        {
            WatchEffect(EffectPhase.Commit, () => { ReactiveScope.TrackAll(trackables); }, false);
        }

        protected void WatchEffect(EffectPhase phase, Action action, bool enableDynamicUnbinding = true)
        {
            var effect = new ReactiveEffect(action, enableDynamicUnbinding);
            if (phase == EffectPhase.Commit) effect.OnInvalidated += () => Invalidate(DirtyType.Visual);
            RegisterResource(effect);
            switch (phase)
            {
                case EffectPhase.State:
                    _stateEffects.Add(effect);
                    break;
                case EffectPhase.Visual:
                    _visualEffects.Add(effect);
                    break;
                case EffectPhase.Commit:
                    _commitEffects.Add(effect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }


        protected ResourceSlot<T> WatchResource<T>(Func<T> factory, bool enableDynamicUnbinding = true)
            where T : class, IDisposable
        {
            var slot = new ResourceSlot<T>(factory, enableDynamicUnbinding);
            RegisterResource(slot);
            return slot;
        }

        public void Update(DataHub hub, TimeSpan elapsed, bool enter)
        {
            Update(hub, UpdateOptions.Default, elapsed, enter);
        }

        public void Update(DataHub hub, UpdateOptions options, TimeSpan elapsed, bool enter)
        {
            InitializeHubAndViewModel(hub, options);
            if (enter) Enter();
            UpdateStates(hub, options, elapsed);
            ResolveBlocking();
            if (IsUpdateBlocked) return;
            ArrangeLayout(options.IgnoreHidden);
            UpdateResources(options.IgnoreHidden);
            CollectDirtyBounds(Vector2.Zero, 1.0f, false, null);
        }

        private void Enter()
        {
            if (_viewModel != null)
            {
                ReactiveScope.BeginBatch();
                try
                {
                    _viewModel.Enter();
                }
                finally
                {
                    ReactiveScope.EndBatch();
                }
            }

            for (var i = 0; i < Children.Count; i++) Children[i].Enter();
        }

        protected virtual void UpdateStates(DataHub hub, UpdateOptions options, TimeSpan elapsed)
        {
            _reseted = false;
            if (_blockingService == null) _blockingService = hub.Get<BlockingService>();
            if (!options.IgnoreHidden && SkipUpdateWhenHidden && !IsVisible) return;
            ReactiveScope.BeginBatch();
            try
            {
                if (_viewModel != null && options.UpdateViewModel) _viewModel.Update(elapsed);
                OnUpdate(elapsed);
            }
            finally
            {
                ReactiveScope.EndBatch();
            }

            IsChildrenLayoutDirty = false;
            IsChildrenVisualDirty = false;
            if (options.IgnoreHidden || !SkipUpdateWhenHidden || IsVisible)
                for (var i = 0; i < _stateEffects.Count; i++)
                {
                    ReactiveScope.BeginBatch();
                    try
                    {
                        _stateEffects[i].Run();
                    }
                    finally
                    {
                        ReactiveScope.EndBatch();
                    }
                }

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (options.IgnoreHidden || child.IsVisible || !child.SkipUpdateWhenHidden)
                    child.UpdateStates(hub, options, elapsed);

                if (child.IsLayoutDirty || child.IsChildrenLayoutDirty) IsChildrenLayoutDirty = true;
                if (child.IsVisualDirty || child.IsChildrenVisualDirty) IsChildrenVisualDirty = true;
            }
        }

        private void ArrangeLayout(bool ignoreHidden)
        {
            if (!ignoreHidden && SkipUpdateWhenHidden && !IsVisible) return;
            if (!IsLayoutDirty && !IsChildrenLayoutDirty && !IsGlobalFirstUpdate) return;
            OnArrangeLayout(ignoreHidden);
        }

        protected virtual void OnArrangeLayout(bool ignoreHidden)
        {
            for (var i = 0; i < Children.Count; i++) ArrangeChild(Children[i], ignoreHidden);
        }

        protected void ArrangeChild(Widget child, bool ignoreHidden)
        {
            if (child == null) return;
            if (!ignoreHidden && !child.IsVisible && child.SkipUpdateWhenHidden) return;
            child.ArrangeLayout(ignoreHidden);
            if (child.IsVisualDirty || child.IsChildrenVisualDirty) IsChildrenVisualDirty = true;
            if (child.IsLayoutDirty || child.IsChildrenLayoutDirty) IsChildrenLayoutDirty = true;
        }

        public void UpdateResources(bool ignoreHidden = false)
        {
            if (!ignoreHidden && SkipUpdateWhenHidden && !IsVisible) return;
            ExecuteVisualPass(ignoreHidden);
            if (!IsVisualDirty && !IsChildrenVisualDirty && !IsGlobalFirstUpdate) return;
            ExecuteCommitPass(ignoreHidden);
        }

        private void ExecuteVisualPass(bool ignoreHidden)
        {
            IsOffScreen = ignoreHidden;
            for (var i = 0; i < _visualEffects.Count; i++)
            {
                ReactiveScope.BeginBatch();
                try
                {
                    _visualEffects[i].Run();
                }
                finally
                {
                    ReactiveScope.EndBatch();
                }
            }

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if (ignoreHidden || child.IsVisible || !child.SkipUpdateWhenHidden)
                    child.ExecuteVisualPass(ignoreHidden);

                // 保险
                if (child.IsVisualDirty || child.IsChildrenVisualDirty) IsChildrenVisualDirty = true;
            }
        }

        private void ExecuteCommitPass(bool ignoreHidden)
        {
            IsOffScreen = ignoreHidden;
            for (var i = 0; i < _commitEffects.Count; i++)
            {
                ReactiveScope.BeginBatch();
                try
                {
                    if (_commitEffects[i].Run()) IsVisibleDirty = true;
                }
                finally
                {
                    ReactiveScope.EndBatch();
                }
            }

            for (var i = 0; i < _resourceSlots.Count; i++)
                if (_resourceSlots[i].Update(false))
                    IsVisibleDirty = true;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (ignoreHidden || child.IsVisible || !child.SkipUpdateWhenHidden)
                    if (child.IsVisualDirty || child.IsChildrenVisualDirty || child.IsGlobalFirstUpdate)
                    {
                        child.ExecuteCommitPass(ignoreHidden);
                        // 保险
                        if (child.IsVisualDirty || child.IsChildrenVisualDirty) IsChildrenVisualDirty = true;
                    }
            }
        }

        protected virtual void CollectDirtyBounds(
            Vector2 parentGlobalPos,
            float parentScale,
            bool parentWillReport,
            float? parentRefreshSpeed
        )
        {
            if (IsGlobalPosition)
            {
                X.Value -= parentGlobalPos.X / parentScale;
                Y.Value -= parentGlobalPos.Y / parentScale;
                IsGlobalPosition = false;
            }

            var refreshSpeed = RefreshSpeed ?? parentRefreshSpeed;
            if (!IsVisible)
            {
                if (!LastGlobalDirtyBounds.IsEmpty)
                {
                    if (!parentWillReport)
                        Context.DirtyUpdateManager.ReportArea(new DirtyArea(LastGlobalDirtyBounds, refreshSpeed ?? 0));

                    LastGlobalDirtyBounds = RectangleF.Empty;
                }

                Reset();
                return;
            }

            var selfGlobalPos = parentGlobalPos + Pos * parentScale;
            var localDeepBounds = GetDirtyBoundsDeep();
            var selfGlobalDeepBounds = RectangleF.Empty;
            if (!localDeepBounds.IsEmpty)
            {
                var totalScale = parentScale * Scale;
                selfGlobalDeepBounds = new RectangleF(
                    selfGlobalPos.X + localDeepBounds.X * totalScale,
                    selfGlobalPos.Y + localDeepBounds.Y * totalScale,
                    localDeepBounds.Width * totalScale,
                    localDeepBounds.Height * totalScale
                );
            }

            var selfWillReport = (IsVisualDirty || IsFirstUpdate) &&
                                 (MergeChildrenDirtyBounds || !SelfRelativeDirtyBounds.IsEmpty ||
                                  !LastGlobalDirtyBounds.IsEmpty);

            if (selfWillReport && !parentWillReport)
            {
                var reportRect = IsFirstUpdate || LastGlobalDirtyBounds.IsEmpty
                    ? selfGlobalDeepBounds
                    : selfGlobalDeepBounds.IsEmpty
                        ? LastGlobalDirtyBounds
                        : RectangleF.Union(selfGlobalDeepBounds, LastGlobalDirtyBounds);

                if (!reportRect.IsEmpty)
                    Context.DirtyUpdateManager.ReportArea(new DirtyArea(reportRect, refreshSpeed ?? 0));
            }

            for (var i = 0; i < Children.Count; i++)
                Children[i].CollectDirtyBounds(selfGlobalPos, parentScale * Scale, parentWillReport || selfWillReport,
                    RefreshSpeed ?? parentRefreshSpeed);

            LastGlobalDirtyBounds = selfGlobalDeepBounds;
            IsGlobalFirstUpdate = false;
            IsFirstUpdate = false;
        }

        public void Render(RectangleF globalClipRect, bool forceRender, bool ignoreHidden)
        {
            Render(globalClipRect, 1, forceRender, ignoreHidden);
        }

        protected virtual void Render(RectangleF globalClipRect, float parentScale, bool forceRender, bool ignoreHidden)
        {
            var shouldRender = (ignoreHidden || IsVisible) &&
                               (forceRender || IsVisualDirty || IsChildrenVisualDirty || IsFirstRender);
            if (!shouldRender) return;
            var totalScale = parentScale * Scale;
            var selfGlobalDeepBounds = LastGlobalDirtyBounds;
            if (!selfGlobalDeepBounds.IsEmpty && !selfGlobalDeepBounds.IntersectsWith(globalClipRect)) return;
            var oldTransform = Context.DeviceContext.Transform;
            var localTransform = Matrix3x2.CreateScale(Scale) * Matrix3x2.CreateTranslation(X, Y);
            Context.DeviceContext.Transform = localTransform * oldTransform;
            IsOffScreen = ignoreHidden;
            OnDraw(totalScale);
            if (!SelfRelativeDirtyBounds.IsEmpty && Context.ShowDebugRect)
            {
                Context.CommonBrush.Color = Colors.Green;
                var rect = SelfRelativeDirtyBounds;
                rect.Inflate(-0.5f, -0.5f);
                Context.DeviceContext.DrawRectangle(rect, Context.CommonBrush);
            }

            for (var i = 0; i < Children.Count; i++) Children[i].Render(globalClipRect, totalScale, true, ignoreHidden);

            Context.DeviceContext.Transform = oldTransform;
        }

        public void ClearDirty()
        {
            ClearStates(true, true, true);
        }

        public void InitStates()
        {
            ClearStates(true, false, true);
        }

        protected virtual void ClearStates(bool clearDirtyStates, bool clearRenderStates, bool parentWasUpdated)
        {
            var wasUpdated = parentWasUpdated && (IsVisible || !SkipUpdateWhenHidden);
            if (clearRenderStates && wasUpdated) IsFirstRender = false;
            if (clearDirtyStates) ClearSelfDirty(parentWasUpdated);
            for (var i = 0; i < Children.Count; i++)
                Children[i].ClearStates(clearDirtyStates, clearRenderStates, wasUpdated);
        }

        private void ClearSelfDirty(bool parentWasUpdated)
        {
            var wasUpdated = parentWasUpdated && (IsVisible || !SkipUpdateWhenHidden);
            if (wasUpdated) ManualIsDirtyMark = false;

            IsLayoutDirty = false;
            IsVisibleDirty = false;
            IsChildrenLayoutDirty = false;
            IsChildrenVisualDirty = false;
        }

        public void StaticWarmUp()
        {
            StaticWarmUp(1);
        }

        private void StaticWarmUp(float parentScale)
        {
            var totalScale = parentScale * Scale;
            var oldTransform = Context.DeviceContext.Transform;
            var localTransform = Matrix3x2.CreateScale(Scale) * Matrix3x2.CreateTranslation(X, Y);
            Context.DeviceContext.Transform = localTransform * oldTransform;
            OnStaticWarmUp(totalScale);
            for (var i = 0; i < Children.Count; i++) Children[i].StaticWarmUp(totalScale);

            Context.DeviceContext.Transform = oldTransform;
        }

        protected virtual void OnStaticWarmUp(float totalScale)
        {
        }

        protected virtual void OnDraw(float totalScale)
        {
        }

        protected virtual void OnInitializeFromHub(DataHub hub)
        {
        }

        protected virtual void OnUpdate(TimeSpan elapsed)
        {
        }

        protected virtual void OnTranslateViewModelStates()
        {
        }

        protected virtual Widget CreateFallbackWidget()
        {
            return null;
        }

        public virtual void Reset()
        {
            if (_hub != null) ViewModel?.Reset();
            _reseted = true;
            IsFirstUpdate = true;
            IsGlobalFirstUpdate = true;
            IsFirstRender = true;
            LastGlobalDirtyBounds = RectangleF.Empty;
            _resolvedBlocks.Clear();
            for (var i = 0; i < Children.Count; i++) Children[i].Reset();
        }

        protected void AddChild(Widget widget, bool isGlobalPosition = false)
        {
            if (widget == null) widget = CreateFallbackWidget();

            if (widget == null) return;
            Children.Add(widget);
            widget.OnInvalidated += OnChildInvalidated;
            if (isGlobalPosition) widget.IsGlobalPosition = true;

            if (_hub != null)
            {
                widget.UpdateStates(_hub, UpdateOptions.Default, TimeSpan.Zero);
                widget.ArrangeLayout(false);
                widget.UpdateResources();
                widget.CollectDirtyBounds(Vector2.Zero, 1f, false, null);
                widget.ClearDirty();
                Invalidate(DirtyType.Layout);
            }
        }

        protected void InsertChild(Widget widget, int index = 0, bool isGlobalPosition = false)
        {
            if (widget == null) widget = CreateFallbackWidget();

            if (widget == null) return;
            Children.Insert(index, widget);
            widget.OnInvalidated += OnChildInvalidated;
            if (isGlobalPosition) widget.IsGlobalPosition = true;
        }

        protected void InsertChildAfter(Widget widget, Widget afterWidget, bool isGlobalPosition = false)
        {
            if (afterWidget == null) return;
            if (widget == null) widget = CreateFallbackWidget();

            if (widget == null) return;
            var index = Children.IndexOf(afterWidget);
            if (index == -1) throw new InvalidOperationException($"{afterWidget.GetType()} not found in children.");
            InsertChild(widget, index + 1, isGlobalPosition);
        }

        protected void RegisterResource(IDisposable resource)
        {
            _disposableStack.AddResource(resource);
            if (resource is IResourceSlot resourceSlot)
            {
                _resourceSlots.Add(resourceSlot);
                resourceSlot.OnInvalidated += () => Invalidate(DirtyType.Visual);
            }
        }

        public IEnumerable<Widget> GetChildrenDeep()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.GetChildrenDeep()) yield return descendant;
            }
        }

        public RectangleF GetDirtyBoundsDeep()
        {
            if (!IsVisible) return RectangleF.Empty;
            if (!_isDeepBoundsDirty && !IsGlobalFirstUpdate) return _cachedDeepBounds;

            var bounds = SelfRelativeDirtyBounds;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible) continue;
                var childLocalDeep = child.GetDirtyBoundsDeep();
                if (childLocalDeep.IsEmpty) continue;
                var childBoundsInParentSpace = new RectangleF(
                    (child.X + childLocalDeep.X) * child.Scale,
                    (child.Y + childLocalDeep.Y) * child.Scale,
                    childLocalDeep.Width * child.Scale,
                    childLocalDeep.Height * child.Scale
                );
                bounds = bounds.IsEmpty ? childBoundsInParentSpace : RectangleF.Union(bounds, childBoundsInParentSpace);
            }

            _isDeepBoundsDirty = false;
            _cachedDeepBounds = bounds;
            return _cachedDeepBounds;
        }


        protected void RequestBlock(string blockType, BlockingLevel level, float duration, Action onComplete)
        {
            if (_blockingService == null) return;
            var entry = new WidgetBlockingEntry { OnComplete = onComplete };
            _blockingService.RequestBlock(blockType, level, entry,
                resolvedEntry =>
                {
                    if (_reseted) return;
                    _resolvedBlocks.Add(resolvedEntry);
                }, duration);
        }

        protected bool IsTypeBlocked(string blockType)
        {
            return _blockingService != null && _blockingService.IsBlocked(blockType);
        }

        private void ResolveBlocking()
        {
            if (_resolvedBlocks.Count > 0)
            {
                for (var i = 0; i < _resolvedBlocks.Count; i++) _resolvedBlocks[i].OnComplete?.Invoke();
                _resolvedBlocks.Clear();
            }

            for (var i = 0; i < Children.Count; i++) Children[i].ResolveBlocking();
        }

        private void InitializeHubAndViewModel(DataHub hub, UpdateOptions options)
        {
            if (_hub != null) return;
            if (options.UpdateViewModel)
            {
                OnInitializeFromHub(hub);
                if (_viewModel != null && !_viewModel.IsInitialized) _viewModel.Initialize(hub);
                _hub = hub;
            }

            for (var i = 0; i < Children.Count; i++) Children[i].InitializeHubAndViewModel(hub, options);
        }

        public bool HandlePointerDown(Vector2 localPoint)
        {
            if (IsPointerDownBlocked || !IsVisible) return false;

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                if (Math.Abs(child.Scale) < Epsilons.FloatEpsilon) continue;
                var childLocalPoint = (localPoint - child.Pos) / child.Scale;
                if (child.HandlePointerDown(childLocalPoint)) return true;
            }

            return IsPointInside(localPoint) && OnPointerDown(localPoint);
        }

        private bool IsPointInside(Vector2 localPoint)
        {
            var boundsList = LocalClickBoundsList;
            for (var i = 0; i < boundsList.Count; i++)
            {
                var bounds = boundsList[i];
                if (bounds.IsEmpty) continue;
                if (bounds.Contains(localPoint.X, localPoint.Y)) return true;
            }

            return false;
        }

        protected virtual bool OnPointerDown(Vector2 localPoint)
        {
            return false;
        }

        public void Exit()
        {
            ViewModel?.Exit();
            for (var i = 0; i < Children.Count; i++) Children[i].Exit();
        }

        protected virtual void OnDispose()
        {
        }

        private class WidgetBlockingEntry
        {
            public Action OnComplete { get; set; }
        }
    }

    public abstract class Widget<TViewModel> : Widget where TViewModel : ViewModel
    {
        protected Widget(RenderContext context, float x = 0, float y = 0, float scale = 1) : base(context, x, y, scale)
        {
        }

        protected new TViewModel ViewModel
        {
            get => (TViewModel)base.ViewModel;
            set => base.ViewModel = value;
        }
    }
}