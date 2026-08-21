using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.DirtyUpdate;
using JREMonitors.Core.Reactive;
using Vortice.Mathematics;

namespace JREMonitors.Core.Widgets
{
    public class WidgetSwitcher : Widget
    {
        public enum RefreshPolicy
        {
            Default,
            MergeDirtyDelayAreas,
            FadeOutThenFadeIn
        }

        public enum RefreshSpeedPreferences
        {
            Fastest,
            Slowest
        }

        private readonly FrameCollector _frameCollector = new FrameCollector();
        private readonly RefreshPolicy _refreshPolicy;
        private readonly RefreshSpeedPreferences _refreshSpeedPreferences;

        private readonly Dictionary<string, Widget> _widgets = new Dictionary<string, Widget>();
        private string _displayedWidgetId;
        private bool _pendingFadeIn;

        public WidgetSwitcher(
            RenderContext context,
            RefreshPolicy refreshPolicy,
            RefreshSpeedPreferences refreshSpeedPreferences = RefreshSpeedPreferences.Fastest,
            IReadOnlyDictionary<string, Widget> widgets = null,
            string initialWidgetId = null
        ) : base(context)
        {
            _refreshPolicy = refreshPolicy;
            _refreshSpeedPreferences = refreshSpeedPreferences;
            ActiveWidgetId = CreatePropertySlot(DirtyType.Visual, initialWidgetId);

            WatchEffect(EffectPhase.State, () =>
            {
                var newId = ActiveWidgetId.Value;
                if (newId == null || !_widgets.TryGetValue(newId, out _)) return;
                if (newId == _displayedWidgetId) return;
                var prevId = _displayedWidgetId;
                _displayedWidgetId = newId;

                if (_refreshPolicy == RefreshPolicy.FadeOutThenFadeIn && prevId != null)
                {
                    _pendingFadeIn = true;
                    foreach (var pair in _widgets)
                        pair.Value.IsVisible.Value = false;
                }
                else
                {
                    foreach (var pair in _widgets)
                        pair.Value.IsVisible.Value = pair.Key == newId;
                }
            }, false);

            if (widgets == null) return;
            foreach (var pair in widgets) Add(pair.Key, pair.Value);
        }

        public PropertySlot<string> ActiveWidgetId { get; }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;


        public string CurrentWidgetId => ActiveWidgetId.Value;

        public void Add(string id, Widget widget)
        {
            AddChild(widget);
            _widgets[id] = widget;
        }

        public void SetActiveWidget(string widgetId)
        {
            ActiveWidgetId.Value = widgetId;
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (!_pendingFadeIn) return;
            var currentId = ActiveWidgetId.Value;
            if (currentId != null && _widgets.TryGetValue(currentId, out var widget))
                widget.IsVisible.Value = true;
            _pendingFadeIn = false;
        }

        protected override void CollectDirtyBounds(Vector2 parentGlobalPos, float parentScale, bool parentWillReport,
            float? parentRefreshSpeed)
        {
            if (_refreshPolicy == RefreshPolicy.Default || _refreshPolicy == RefreshPolicy.FadeOutThenFadeIn)
            {
                base.CollectDirtyBounds(parentGlobalPos, parentScale, parentWillReport, parentRefreshSpeed);
                return;
            }

            var oldReporter = Context.DirtyUpdateManager.ActiveReporter;
            Context.DirtyUpdateManager.ActiveReporter = _frameCollector;
            base.CollectDirtyBounds(parentGlobalPos, parentScale, parentWillReport, parentRefreshSpeed);
            Context.DirtyUpdateManager.ActiveReporter = oldReporter;
            var immCount = _frameCollector.Immediate.Count;
            for (var i = 0; i < immCount; i++) Context.DirtyUpdateManager.ReportArea(_frameCollector.Immediate[i]);

            var delayCount = _frameCollector.Delay.Count;
            var delayRect = RectangleF.Empty;
            float refreshSpeed = 0;
            var hasValidArea = false;
            for (var i = 0; i < delayCount; i++)
            {
                var area = _frameCollector.Delay[i];
                if (area.Rect.IsEmpty) continue;
                if (!hasValidArea)
                {
                    delayRect = area.Rect;
                    refreshSpeed = area.RefreshSpeed;
                    hasValidArea = true;
                }
                else
                {
                    refreshSpeed = _refreshSpeedPreferences == RefreshSpeedPreferences.Fastest
                        ? MathHelper.Min(refreshSpeed, area.RefreshSpeed)
                        : MathHelper.Max(refreshSpeed, area.RefreshSpeed);
                    delayRect = RectangleF.Union(delayRect, area.Rect);
                }
            }

            if (hasValidArea) Context.DirtyUpdateManager.ReportArea(new DirtyArea(delayRect, refreshSpeed));

            _frameCollector.Clear();
        }

        public override void Reset()
        {
            base.Reset();
            _displayedWidgetId = ActiveWidgetId.Value;
            _pendingFadeIn = false;
            var currentId = ActiveWidgetId.Value;
            if (currentId != null && _widgets.TryGetValue(currentId, out var widget))
                widget.IsVisible.Value = true;
        }

        protected override void OnDispose()
        {
            _frameCollector.Clear();
            foreach (var widget in _widgets.Values) widget.Dispose();

            _widgets.Clear();
        }
    }
}