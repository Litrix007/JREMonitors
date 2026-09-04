using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using Vortice.Mathematics;

namespace JREMonitors.Core.Monitors
{
    public abstract class Screen : IDisposable
    {
        protected Screen(
            IList<string> availableIds,
            Size size,
            float backgroundRefreshSpeed,
            bool clearWhenSwitchingTo,
            bool isBackgroundStatic = true
        )
        {
            if (availableIds.Count == 0) throw new ArgumentException($"{nameof(availableIds)} must not be empty");

            AvailableIds = availableIds.ToArray();
            Size = size;
            BackgroundRefreshSpeed = backgroundRefreshSpeed;
            ClearWhenSwitchingTo = clearWhenSwitchingTo;
            IsBackgroundStatic = isBackgroundStatic;
        }

        protected Screen(
            string id,
            Size size,
            float backgroundRefreshSpeed,
            bool clearWhenSwitchingTo,
            bool isBackgroundStatic = true
        ) : this(new[] { id }, size, backgroundRefreshSpeed, clearWhenSwitchingTo, isBackgroundStatic)
        {
        }

        public string[] AvailableIds { get; }
        public Size Size { get; }
        public float BackgroundRefreshSpeed { get; }
        public virtual Color4 BackgroundColor => default;
        public bool ClearWhenSwitchingTo { get; }
        public bool IsBackgroundStatic { get; }
        protected Widget BackgroundRoot { get; set; }
        protected Widget ForegroundRoot { get; set; }
        public bool HasBackgroundRoot => BackgroundRoot != null;

        public virtual void Dispose()
        {
            BackgroundRoot?.Dispose();
            ForegroundRoot?.Dispose();
        }

        public void Init()
        {
            BackgroundRoot?.InitStates();
            ForegroundRoot?.InitStates();
        }

        public void StaticWarmUpBackground()
        {
            BackgroundRoot?.StaticWarmUp();
        }

        public void StaticWarmUpForeground()
        {
            ForegroundRoot?.StaticWarmUp();
        }

        public void RenderBackground(bool forceRender = true, bool ignoreHidden = false)
        {
            BackgroundRoot?.Render(new RectangleF(PointF.Empty, Size), forceRender, ignoreHidden);
        }

        public void RenderForeground(RectangleF area, bool ignoreHidden = false)
        {
            ForegroundRoot?.Render(area, false, ignoreHidden);
        }

        public bool ForegroundPointerDown(Vector2 point)
        {
            return ForegroundRoot != null && ForegroundRoot.HandlePointerDown(point);
        }

        public void UpdateBackground(DataHub hub, TimeSpan elapsed, bool enter)
        {
            BackgroundRoot?.Update(hub, elapsed, enter);
        }

        public void UpdateBackground(DataHub hub, UpdateOptions options, TimeSpan elapsed, bool enter)
        {
            BackgroundRoot?.Update(hub, options, elapsed, enter);
        }

        public void UpdateForeground(DataHub hub, TimeSpan elapsed, bool enter)
        {
            ForegroundRoot?.Update(hub, elapsed, enter);
        }

        public void UpdateForeground(DataHub hub, UpdateOptions options, TimeSpan elapsed, bool enter)
        {
            ForegroundRoot?.Update(hub, options, elapsed, enter);
        }

        public bool IsBackgroundDirty()
        {
            return BackgroundRoot != null && (BackgroundRoot.IsDirty || BackgroundRoot.IsChildrenLayoutDirty ||
                                              BackgroundRoot.IsChildrenVisualDirty);
        }

        public void ClearBackgroundDirty()
        {
            BackgroundRoot?.ClearDirty();
        }

        public void ClearForegroundDirty()
        {
            ForegroundRoot?.ClearDirty();
        }

        public void ExitBackground()
        {
            BackgroundRoot?.Exit();
        }

        public void ExitForeground()
        {
            ForegroundRoot?.Exit();
        }

        public void ResetBackground()
        {
            BackgroundRoot?.Reset();
        }

        public void ResetForeground()
        {
            ForegroundRoot?.Reset();
        }

        public void Reset()
        {
            ResetBackground();
            ResetForeground();
        }
    }
}