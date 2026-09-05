using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.DirtyUpdate;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Monitors
{
    /// <summary>
    ///     监视器状态机，管理屏幕集合、当前活动屏幕与渲染帧循环。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         持有独立的 <see cref="RenderContext" /> 与本地 <see cref="DataHub" />；Screen 由工厂创建并按
    ///         AvailableIds 注册。可挂载多个 <see cref="MonitorOutput" /> 输出，各输出均绘制同一屏内容。
    ///     </para>
    ///     <para>
    ///         帧循环由 <see cref="MonitorDrawState" /> 状态机驱动：背景按即时或行扫描延迟
    ///         （<c>BackgroundDelayed</c>）方式写入各输出的背景缓冲；前景更新阶段经
    ///         <see cref="DirtyUpdateManager" /> 收集脏区，即时脏区直接局部重绘（<c>DrawImmediate</c>），
    ///         低刷新率内容在离屏 <c>DelayedBitmap</c> 上渲染后按刷新速度分片同步（<c>DrawDelayed</c> →
    ///         <c>SyncDelayed</c>）；带残影的输出再叠加 EMA 状态合成。
    ///     </para>
    ///     <para>
    ///         点击输入经 <c>Click</c> 将物理坐标映射为活动屏幕逻辑坐标后派发；两阶段
    ///         （<see cref="AddOutput" /> 的 <c>WarmUpPhase1</c> 与首帧 <c>WarmUpPhase2</c>）完成离屏静态内容预热。
    ///     </para>
    /// </remarks>
    public class Monitor : IDisposable
    {
        private readonly BlockingService _blockingService;
        private readonly IDebugger _debugger;
        private readonly FrameCollector _frameCollector = new FrameCollector();
        private readonly List<MonitorOutput> _outputs = new List<MonitorOutput>();
        private readonly RenderContext _renderContext;
        private readonly Dictionary<string, Screen> _screens = new Dictionary<string, Screen>();
        private readonly Func<bool> _showDebugRectGetter;
        private readonly Stopwatch _sw = new Stopwatch();
        private Screen _activeScreen;
        private bool _canBackgroundReset;
        private int _copiedRowCount;
        private int _delayIndex;
        private bool _firstRenderCompleted;
        private bool _hasFirstOffScreenRenderCompleted;
        private bool _hasScreenInitialized;
        private MonitorDrawState _monitorDrawState = MonitorDrawState.BackgroundImmediate;
        private Vector2 _physicalScale;
        private bool _shouldEnterBackground = true;
        private bool _shouldEnterForeground = true;
        private bool _suppressDebugRect;

        public Monitor(
            string id,
            DataHub dataHub,
            MonitorContext context,
            Func<RenderContext, IEnumerable<Screen>> screensFactory,
            string initialScreenId = null,
            Func<bool> showDebugRectGetter = null
        )
        {
            _debugger = context.Debugger;
            _showDebugRectGetter = showDebugRectGetter;
            _renderContext = new RenderContext(context,
                () => ShowDebugRect,
                () => _physicalScale,
                () => ActiveScreenId);
            LocalDataHub = new DataHub(dataHub);
            _blockingService = dataHub.Get<BlockingService>();
            _blockingService.RegisterMonitor(id, () => _activeScreen?.AvailableIds);
            var screens = screensFactory(_renderContext);
            foreach (var screen in screens)
            {
                var availableIds = screen.AvailableIds;
                foreach (var screenId in availableIds) _screens.Add(screenId, screen);
            }

            if (initialScreenId == null)
            {
                _activeScreen = _screens.Values.FirstOrDefault();
                if (_activeScreen != null) ActiveScreenId = _activeScreen.AvailableIds[0];
            }
            else
            {
                if (_screens.TryGetValue(initialScreenId, out _activeScreen)) ActiveScreenId = initialScreenId;
            }

            if (_activeScreen != null && _activeScreen.BackgroundRefreshSpeed > 0)
                _monitorDrawState = MonitorDrawState.BackgroundDelayed;
            Id = id;
        }

        private bool ShowDebugRect => _showDebugRectGetter != null && _showDebugRectGetter() && !_suppressDebugRect;
        public string Id { get; }
        public string ActiveScreenId { get; set; }
        public DataHub LocalDataHub { get; }
        public float Brightness => _renderContext.DisplayController.Brightness;

        public void Dispose()
        {
            _blockingService.RemoveMonitor(Id);
            foreach (var output in _outputs) output.Dispose();
            _outputs.Clear();
            ExitActiveScreen();
            foreach (var screen in _screens.Values.Distinct()) screen.Dispose();
            LocalDataHub.Dispose();
        }

        public void AddOutput(MonitorOutput output)
        {
            _outputs.Add(output);
            _renderContext.DeviceContext.BeginDraw();
            var oldReporter = _renderContext.DirtyUpdateManager.ActiveReporter;
            _renderContext.DirtyUpdateManager.ActiveReporter = null;
            foreach (var screen in _screens.Values)
            {
                _sw.Restart();
                WarmUpPhase1(screen, output);
                if (screen.HasBackgroundRoot && screen.IsBackgroundStatic)
                {
                    _suppressDebugRect = true;
                    var buffer = output.GetOrCreateBuffer(screen, _renderContext.DeviceContext);
                    RenderBackgroundToScreenBuffer(output, screen, buffer);
                }

                screen.Reset();
                _sw.Stop();
                _debugger?.AddLineLasting(
                    $"warm up phase1 {string.Join(",", screen.AvailableIds)} {_sw.Elapsed.TotalMilliseconds}ms");
            }

            _renderContext.DeviceContext.Target = null;
            _renderContext.DirtyUpdateManager.ActiveReporter = oldReporter;
            _renderContext.DeviceContext.EndDraw();
            _hasFirstOffScreenRenderCompleted = false;
        }

        public bool RemoveOutput(MonitorOutput output)
        {
            return _outputs.Remove(output);
        }

        public void ResetActiveScreen(bool selfBeginAndEndDraw)
        {
            _activeScreen?.ClearForegroundDirty();
            _activeScreen?.ResetForeground();
            ResetStates();
            if (_activeScreen != null && _activeScreen.ClearWhenSwitchingTo) ClearActiveScreens(selfBeginAndEndDraw);
        }

        private void RenderBackgroundToScreenBuffer(MonitorOutput output, Screen screen,
            ScreenBackgroundBuffer buffer)
        {
            buffer.HasBackgroundRendered = true;
            buffer.ShowDebugRect = ShowDebugRect;
            var dc = _renderContext.DeviceContext;
            dc.Target = buffer.BackgroundBitmap;
            dc.Clear(screen.BackgroundColor);
            WithPhysicalScale(output, screen.Size, () => screen.RenderBackground());
            dc.Target = null;
        }

        private void WarmUpPhase1(Screen screen, MonitorOutput output)
        {
            var dc = _renderContext.DeviceContext;
            WithPhysicalScale(output, screen.Size, () =>
            {
                if (screen.HasBackgroundRoot && !screen.IsBackgroundStatic)
                    screen.UpdateBackground(LocalDataHub, UpdateOptions.WarmUp, TimeSpan.Zero, false);
                screen.UpdateForeground(LocalDataHub, UpdateOptions.WarmUp, TimeSpan.Zero, false);

                if (screen.HasBackgroundRoot && !screen.IsBackgroundStatic)
                {
                    var buffer = output.GetOrCreateBuffer(screen, dc);
                    dc.Target = buffer.BackgroundBitmap;

                    screen.StaticWarmUpBackground();
                }

                dc.Target = output.MonitorRenderTargetBitmap;
                screen.StaticWarmUpForeground();
            });
        }

        private void WarmUpPhase2(Screen screen, MonitorOutput output)
        {
            WithPhysicalScale(output, screen.Size, () =>
            {
                var dc = _renderContext.DeviceContext;
                if (screen.HasBackgroundRoot && !screen.IsBackgroundStatic)
                    screen.UpdateBackground(LocalDataHub, UpdateOptions.FirstOffScreenRender, TimeSpan.Zero, false);
                screen.UpdateForeground(LocalDataHub, UpdateOptions.FirstOffScreenRender, TimeSpan.Zero, false);
                if (screen.HasBackgroundRoot && !screen.IsBackgroundStatic)
                {
                    var buffer = output.GetOrCreateBuffer(screen, dc);
                    dc.Target = buffer.BackgroundBitmap;
                    dc.Clear(screen.BackgroundColor);
                    screen.RenderBackground(true, true);
                }

                dc.Target = output.MonitorRenderTargetBitmap;
                screen.RenderForeground(new RectangleF(PointF.Empty, screen.Size), true);
            });
            screen.Reset();
        }

        private void WithPhysicalScale(MonitorOutput output, Size logicalSize, Action action)
        {
            var scaleX = (float)output.Width / logicalSize.Width;
            var scaleY = (float)output.Height / logicalSize.Height;
            _physicalScale = new Vector2(scaleX, scaleY);
            var dc = _renderContext.DeviceContext;
            var oldTransform = dc.Transform;
            dc.Transform = Matrix3x2.CreateScale(scaleX, scaleY) * oldTransform;
            try
            {
                action();
            }
            finally
            {
                dc.Transform = oldTransform;
            }
        }

        private void RenderForegroundClipped(ID2D1Bitmap1 backgroundBitmap, RectangleF physicalRect, float scaleX,
            float scaleY)
        {
            var dc = _renderContext.DeviceContext;
            dc.DrawBitmap(backgroundBitmap, physicalRect, 1,
                BitmapInterpolationMode.NearestNeighbor, physicalRect);
            dc.PushAxisAlignedClip(physicalRect, AntialiasMode.Aliased);
            var logLeft = (float)Math.Floor(physicalRect.Left / scaleX);
            var logTop = (float)Math.Floor(physicalRect.Top / scaleY);
            var logRight = (float)Math.Ceiling(physicalRect.Right / scaleX);
            var logBottom = (float)Math.Ceiling(physicalRect.Bottom / scaleY);
            var conservativeLogicalRect = new RectangleF(
                logLeft,
                logTop,
                logRight - logLeft,
                logBottom - logTop
            );

            var oldTransform = dc.Transform;
            dc.Transform = Matrix3x2.CreateScale(scaleX, scaleY) * oldTransform;
            _suppressDebugRect = false;
            _activeScreen.RenderForeground(conservativeLogicalRect);
            dc.Transform = oldTransform;
            dc.PopAxisAlignedClip();
        }

        private void ClearActiveScreens(bool selfBeginAndEndDraw)
        {
            if (_activeScreen == null) return;
            if (selfBeginAndEndDraw) _renderContext.DeviceContext.BeginDraw();
            foreach (var output in _outputs) output.ClearActiveScreen(_renderContext.DeviceContext);
            if (selfBeginAndEndDraw) _renderContext.DeviceContext.EndDraw();
        }

        private void HandleDynamicBackground(TimeSpan elapsed, bool selfBeginAndEndDraw)
        {
            if (!_activeScreen.HasBackgroundRoot || _activeScreen.IsBackgroundStatic) return;
            _sw.Restart();
            _activeScreen.UpdateBackground(LocalDataHub, elapsed, _shouldEnterBackground);
            _shouldEnterBackground = false;
            _sw.Stop();
            _debugger?.AddLine($"{Id} update dynamic background {_sw.Elapsed.TotalMilliseconds}ms");
            var shouldResetScreen = false;
            for (var i = 0; i < _outputs.Count; i++)
            {
                var output = _outputs[i];
                var buffer = output.GetOrCreateBuffer(_activeScreen, _renderContext.DeviceContext);
                var showDebugRect = ShowDebugRect;
                var showDebugRectChanged = buffer.ShowDebugRect != showDebugRect;
                var prevHasBackgroundRendered = buffer.HasBackgroundRendered;
                var isBackgroundDirty = _activeScreen.IsBackgroundDirty();
                var backgroundColorChanged = buffer.LastBackgroundColor != _activeScreen.BackgroundColor;
                if (!prevHasBackgroundRendered || isBackgroundDirty || showDebugRectChanged || backgroundColorChanged)
                {
                    _sw.Restart();
                    _suppressDebugRect = false;
                    RenderBackgroundToScreenBuffer(output, _activeScreen, buffer);
                    buffer.LastBackgroundColor = _activeScreen.BackgroundColor;
                    _sw.Stop();
                    _debugger?.AddLineLasting($"{Id} render dynamic background {_sw.Elapsed.TotalMilliseconds}ms");
                    if (prevHasBackgroundRendered && _canBackgroundReset) shouldResetScreen = true;
                }
            }

            if (shouldResetScreen) ResetActiveScreen(selfBeginAndEndDraw);

            _canBackgroundReset = true;
            _activeScreen.ClearBackgroundDirty();
        }

        private void HandlePureColorBackground(bool selfBeginAndEndDraw)
        {
            if (_activeScreen.HasBackgroundRoot) return;
            var backgroundColor = _activeScreen.BackgroundColor;
            var shouldResetScreen = false;
            for (var i = 0; i < _outputs.Count; i++)
            {
                var output = _outputs[i];
                var prevHasBackgroundRendered = output.LastBackgroundColor.HasValue;
                if (prevHasBackgroundRendered && output.LastBackgroundColor.Value == backgroundColor) continue;
                _renderContext.DeviceContext.Target = output.PureColorBackgroundBitmap;
                _renderContext.DeviceContext.Clear(backgroundColor);
                _renderContext.DeviceContext.Target = null;
                output.LastBackgroundColor = backgroundColor;
                if (prevHasBackgroundRendered && _canBackgroundReset) shouldResetScreen = true;
            }

            if (shouldResetScreen) ResetActiveScreen(selfBeginAndEndDraw);
            _canBackgroundReset = true;
        }

        private void BackToIdle()
        {
            _monitorDrawState = MonitorDrawState.Idle;
            _delayIndex = 0;
            _firstRenderCompleted = true;
            _activeScreen.ClearForegroundDirty();
            _frameCollector.Clear();
        }

        public void Draw(TimeSpan elapsed, bool selfBeginAndEndDraw = true)
        {
            if (_activeScreen == null || _outputs.Count == 0) return;
            if (!_hasScreenInitialized)
                foreach (var screen in _screens.Values)
                    screen.Init();
            _hasScreenInitialized = true;

            if (!_hasFirstOffScreenRenderCompleted)
            {
                _hasFirstOffScreenRenderCompleted = true;
                var oldReporter = _renderContext.DirtyUpdateManager.ActiveReporter;
                _renderContext.DirtyUpdateManager.ActiveReporter = null;
                if (selfBeginAndEndDraw) _renderContext.DeviceContext.BeginDraw();
                foreach (var screen in _screens.Values)
                    for (var i = 0; i < _outputs.Count; i++)
                    {
                        var output = _outputs[i];
                        _sw.Restart();
                        WarmUpPhase2(screen, output);
                        _sw.Stop();
                        _debugger?.AddLineLasting(
                            $"{Id} warm up phase2 {string.Join(",", screen.AvailableIds)} {_sw.Elapsed.TotalMilliseconds}ms");
                    }

                // 清除预热残余
                var dc = _renderContext.DeviceContext;
                foreach (var output in _outputs)
                {
                    dc.Target = output.MonitorRenderTargetBitmap;
                    dc.Clear(Colors.Black);
                }

                dc.Target = null;
                if (selfBeginAndEndDraw) _renderContext.DeviceContext.EndDraw();
                _renderContext.DirtyUpdateManager.ActiveReporter = oldReporter;
            }

            _blockingService.SetCurrentMonitor(Id);
            HandleDynamicBackground(elapsed, selfBeginAndEndDraw);
            HandlePureColorBackground(selfBeginAndEndDraw);
            var elapsedSeconds = (float)elapsed.TotalSeconds;
            var logicalScreenSize = _activeScreen.Size;
            var logicalScreenBounds = new RectangleF(PointF.Empty, logicalScreenSize);
            var updated = false;
            var doBackgroundImmediate = false;
            var doBackgroundDelayed = false;
            var bgCopyStart = 0;
            var bgCopyCount = 0;
            var shouldBackToIdle = false;

            if (_monitorDrawState == MonitorDrawState.BackgroundImmediate)
            {
                doBackgroundImmediate = true;
                _monitorDrawState = MonitorDrawState.Idle;
                updated = true;
            }
            else if (_monitorDrawState == MonitorDrawState.BackgroundDelayed)
            {
                var count = (int)Math.Ceiling(logicalScreenSize.Height / _activeScreen.BackgroundRefreshSpeed *
                                              elapsedSeconds);
                var remainingRows = logicalScreenSize.Height - _copiedRowCount;
                if (count > remainingRows) count = remainingRows;

                doBackgroundDelayed = count > 0;
                bgCopyStart = _copiedRowCount;
                bgCopyCount = count;

                _copiedRowCount += count;
                updated = true;
                if (_copiedRowCount >= logicalScreenSize.Height)
                {
                    _copiedRowCount = 0;
                    _monitorDrawState = MonitorDrawState.Idle;
                }
            }

            if (_monitorDrawState == MonitorDrawState.Idle)
            {
                _sw.Restart();
                _renderContext.DirtyUpdateManager.ActiveReporter = _frameCollector;
                _activeScreen.UpdateForeground(LocalDataHub, elapsed, _shouldEnterForeground);
                _shouldEnterForeground = false;
                _renderContext.DirtyUpdateManager.ActiveReporter = null;
                _sw.Stop();
                _debugger?.AddLine($"{Id} update foreground {_sw.Elapsed.TotalMilliseconds}ms");
                if (_renderContext.DisplayController.ConsumeReset())
                {
                    ResetActiveScreen(selfBeginAndEndDraw);
                    return;
                }

                if (_renderContext.DisplayController.ConsumeChangeScreen(out var screen, out var forceReset))
                {
                    ChangeScreen(screen, selfBeginAndEndDraw, forceReset);
                    return;
                }

                if (_frameCollector.Immediate.Count > 0 || _frameCollector.Delay.Count > 0)
                {
                    _monitorDrawState = MonitorDrawState.DrawImmediate;
                    updated = true;
                }
                else
                {
                    shouldBackToIdle = true;
                }
            }
            else if (_monitorDrawState != MonitorDrawState.BackgroundImmediate &&
                     _monitorDrawState != MonitorDrawState.BackgroundDelayed)
            {
                updated = true;
            }

            for (var i = 0; i < _outputs.Count; i++)
                _outputs[i].UpdateFreezeState(updated, elapsed);

            var doImmediate = false;
            var doDrawDelayed = false;

            if (_monitorDrawState == MonitorDrawState.DrawImmediate)
            {
                _monitorDrawState = MonitorDrawState.DrawDelayed;
                doImmediate = true;
            }

            if (_monitorDrawState == MonitorDrawState.DrawDelayed)
            {
                if (_frameCollector.Delay.Count > 0)
                {
                    _monitorDrawState = MonitorDrawState.SyncDelayed;
                    doDrawDelayed = true;
                }
                else
                {
                    shouldBackToIdle = true;
                }
            }

            var physicalSliceActions = new List<RectangleF>();

            if (_monitorDrawState == MonitorDrawState.SyncDelayed)
            {
                var remainingElapsed = elapsedSeconds;
                var scaleX = (float)_outputs[0].Width / logicalScreenSize.Width;
                var scaleY = (float)_outputs[0].Height / logicalScreenSize.Height;

                while (remainingElapsed > 0)
                    if (_delayIndex < _frameCollector.Delay.Count)
                    {
                        var area = _frameCollector.Delay[_delayIndex];
                        var areaRect = RectangleF.Intersect(logicalScreenBounds, area.Rect);
                        if (areaRect.IsEmpty)
                        {
                            _delayIndex++;
                            _copiedRowCount = 0;
                            continue;
                        }

                        var fullPhysicalRect = ScaleRectAndSnap(areaRect, scaleX, scaleY);
                        var totalPhysicalRows = (int)fullPhysicalRect.Height;

                        var physicalPixelsPerSecond =
                            _outputs[0].Width * _outputs[0].Height / area.RefreshSpeed;
                        var physicalRowsCanDraw =
                            (int)Math.Ceiling(physicalPixelsPerSecond * remainingElapsed / fullPhysicalRect.Width);
                        var remainingPhysicalRows = totalPhysicalRows - _copiedRowCount;

                        var count = Math.Min(physicalRowsCanDraw, remainingPhysicalRows);
                        if (count <= 0) break;
                        var sliceTop = fullPhysicalRect.Top + _copiedRowCount;
                        var slicePhysicalRect = new RectangleF(fullPhysicalRect.Left, sliceTop, fullPhysicalRect.Width,
                            count);
                        physicalSliceActions.Add(slicePhysicalRect);

                        _copiedRowCount += count;
                        var timeSpent = count * fullPhysicalRect.Width / physicalPixelsPerSecond;
                        remainingElapsed -= timeSpent;

                        if (_copiedRowCount < totalPhysicalRows) continue;

                        _copiedRowCount = 0;
                        _delayIndex++;
                    }
                    else
                    {
                        shouldBackToIdle = true;
                        break;
                    }
            }

            _renderContext.DeviceContext.PrimitiveBlend = PrimitiveBlend.SourceOver;
            if (selfBeginAndEndDraw) _renderContext.DeviceContext.BeginDraw();

            for (var i = 0; i < _outputs.Count; i++)
            {
                var output = _outputs[i];
                if (output.Frozen && !output.JustFrozen) continue;

                var backgroundBitmap = _activeScreen.HasBackgroundRoot
                    ? output.GetOrCreateBuffer(_activeScreen, _renderContext.DeviceContext).BackgroundBitmap
                    : output.PureColorBackgroundBitmap;

                var renderTarget = output.MonitorRenderTargetBitmap;
                var delayedBitmap = output.DelayedBitmap;

                var scaleX = (float)output.Width / logicalScreenSize.Width;
                var scaleY = (float)output.Height / logicalScreenSize.Height;
                _physicalScale = new Vector2(scaleX, scaleY);
                if ((doBackgroundImmediate || doBackgroundDelayed) && !_activeScreen.HasBackgroundRoot)
                {
                    _renderContext.DeviceContext.Target = backgroundBitmap;
                    _renderContext.DeviceContext.Clear(_activeScreen.BackgroundColor);
                }

                _renderContext.DeviceContext.Target = renderTarget;

                if (doBackgroundImmediate)
                {
                    _renderContext.DeviceContext.DrawBitmap(backgroundBitmap, 1,
                        BitmapInterpolationMode.NearestNeighbor);
                }
                else if (doBackgroundDelayed)
                {
                    var logicalRect = new RectangleF(0, bgCopyStart, logicalScreenSize.Width, bgCopyCount);
                    var physicalRect = ScaleRectAndSnap(logicalRect, scaleX, scaleY);
                    _renderContext.DeviceContext.DrawBitmap(backgroundBitmap, physicalRect, 1,
                        BitmapInterpolationMode.NearestNeighbor, physicalRect);
                }

                if (doImmediate)
                {
                    _sw.Restart();
                    for (var j = 0; j < _frameCollector.Immediate.Count; j++)
                    {
                        var area = _frameCollector.Immediate[j];
                        var logicalRect = RectangleF.Intersect(logicalScreenBounds, area.Rect);
                        var physicalRect = ScaleRectAndSnap(logicalRect, scaleX, scaleY);
                        if (physicalRect.IsEmpty) continue;
                        RenderForegroundClipped(backgroundBitmap, physicalRect, scaleX, scaleY);
                    }

                    _sw.Stop();
                    _debugger?.AddLine($"{Id} render foreground immediate{_sw.Elapsed.TotalMilliseconds}ms");
                }

                if (doDrawDelayed)
                {
                    _sw.Restart();
                    _renderContext.DeviceContext.Target = delayedBitmap;
                    for (var j = 0; j < _frameCollector.Delay.Count; j++)
                    {
                        var area = _frameCollector.Delay[j];
                        var logicalRect = RectangleF.Intersect(logicalScreenBounds, area.Rect);
                        var physicalRect = ScaleRectAndSnap(logicalRect, scaleX, scaleY);
                        if (physicalRect.IsEmpty) continue;
                        RenderForegroundClipped(backgroundBitmap, physicalRect, scaleX, scaleY);
                    }

                    _renderContext.DeviceContext.Target = renderTarget;
                    _sw.Stop();
                    _debugger?.AddLine($"{Id} render foreground delayed{_sw.Elapsed.TotalMilliseconds}ms");
                }

                if (physicalSliceActions.Count > 0)
                    for (var j = 0; j < physicalSliceActions.Count; j++)
                    {
                        var physicalRect = physicalSliceActions[j];
                        if (physicalRect.IsEmpty) continue;
                        _renderContext.DeviceContext.DrawBitmap(delayedBitmap, physicalRect, 1,
                            BitmapInterpolationMode.NearestNeighbor, physicalRect);
                    }

                if (output.HasGhosting)
                {
                    var alpha = 1f;
                    if (output.GhostingDecayTimeSeconds > 0 && elapsedSeconds > 0)
                    {
                        alpha = (float)Math.Pow(1f / 255, elapsedSeconds / output.GhostingDecayTimeSeconds);
                        alpha = MathHelper.Clamp(alpha, 0f, 1f);
                    }

                    // GhostingEffect ping-pong 更新：纯 EMA + 带内收敛收缩写 StateNew，随后 parity 互换
                    // 收敛带半径覆盖 8bit round 停滞区（0.5/(1−α) 灰阶），下限 4 灰阶；
                    var justFrozen = output.JustFrozen;
                    var ghostingEffect = output.GhostingEffect;
                    var convergeBand = elapsedSeconds > 0f
                        ? Math.Max(0.6f / Math.Max(1f - alpha, 0.02f), 4f) / 255f
                        : 0f;
                    _renderContext.DeviceContext.Target = output.PendingStateBitmap;
                    ghostingEffect.SetInput(0, renderTarget, true);
                    ghostingEffect.SetInput(1, output.OutputBitmap, true);
                    ghostingEffect.UpdateConstants(justFrozen ? 0f : alpha, justFrozen ? 0f : convergeBand);
                    _renderContext.DeviceContext.DrawImage(ghostingEffect);
                    output.CommitStateSwap();
                }
            }

            if (shouldBackToIdle) BackToIdle();
            if (selfBeginAndEndDraw) _renderContext.DeviceContext.EndDraw();
            _renderContext.DeviceContext.Target = null;
            _blockingService.ClearCurrentMonitor();
        }

        private static RectangleF ScaleRectAndSnap(RectangleF logicalRect, float scaleX, float scaleY)
        {
            var left = (float)Math.Floor(logicalRect.Left * scaleX);
            var top = (float)Math.Floor(logicalRect.Top * scaleY);
            var right = (float)Math.Ceiling(logicalRect.Right * scaleX);
            var bottom = (float)Math.Ceiling(logicalRect.Bottom * scaleY);
            return new RectangleF(left, top, right - left, bottom - top);
        }

        public bool Click(Size outputSize, Vector2 physicalPos)
        {
            if (!_firstRenderCompleted || _activeScreen == null) return false;
            _blockingService.SetCurrentMonitor(Id);
            var logicalPoint = new Vector2(
                physicalPos.X * _activeScreen.Size.Width / outputSize.Width,
                physicalPos.Y * _activeScreen.Size.Height / outputSize.Height
            );
            var result = _activeScreen.ForegroundPointerDown(logicalPoint);
            _blockingService.ClearCurrentMonitor();
            return result;
        }

        private void ResetStates()
        {
            _firstRenderCompleted = false;
            _canBackgroundReset = false;
            _monitorDrawState = _activeScreen == null ? MonitorDrawState.BackgroundImmediate :
                _activeScreen.BackgroundRefreshSpeed > 0 ? MonitorDrawState.BackgroundDelayed :
                MonitorDrawState.BackgroundImmediate;
            _delayIndex = 0;
            _copiedRowCount = 0;
            _frameCollector.Clear();
            foreach (var output in _outputs) output.ResetFreezeState();
        }

        private void ExitActiveScreen()
        {
            if (_activeScreen != null && _activeScreen.HasBackgroundRoot && !_activeScreen.IsBackgroundStatic &&
                !_shouldEnterBackground)
                _activeScreen.ExitBackground();

            if (!_shouldEnterForeground) _activeScreen?.ExitForeground();
        }

        private void ChangeScreen(string screenId, bool selfBeginAndEndDraw, bool forceReset)
        {
            if (screenId == null || (!forceReset && ActiveScreenId == screenId)) return;
            if (!_screens.TryGetValue(screenId, out var targetScreen)) return;
            ActiveScreenId = screenId;
            if (!forceReset && _activeScreen == targetScreen) return;
            _activeScreen?.ClearForegroundDirty();
            ExitActiveScreen();
            _activeScreen?.ResetForeground();
            _activeScreen = targetScreen;
            ResetStates();
            _shouldEnterBackground = true;
            _shouldEnterForeground = true;
            if (targetScreen.ClearWhenSwitchingTo) ClearActiveScreens(selfBeginAndEndDraw);
        }

        public void ChangeScreen(string screenId, bool forceReset = false)
        {
            ChangeScreen(screenId, true, forceReset);
        }

        public void Reset()
        {
            foreach (var screen in _screens.Values.Distinct()) screen.Reset();

            foreach (var output in _outputs) output.ResetBuffers();

            ResetStates();
        }
    }
}