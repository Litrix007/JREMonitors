using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using Microsoft.Win32.SafeHandles;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DXGI;
using Vortice.Mathematics;
using BitmapInterpolationMode = Vortice.Direct2D1.BitmapInterpolationMode;
using Color = System.Drawing.Color;

namespace JREMonitors.Core.Monitors
{
    public class ExternalDisplayForm : IDisposable
    {
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_PAINT = 0x000F;
        private const int WM_CLOSE = 0x0010;
        private const int WM_QUIT = 0x0012;
        private const int WM_USER_PRESENT = 0x0400 + 1;
        private const int WM_USER_HIDE = 0x0400 + 2;
        private const int WM_USER_SHOW = 0x0400 + 3;

        private readonly MonitorContext _context;
        private readonly bool _isTopMost;
        private readonly IntPtr _parentHandle;
        private readonly List<Size> _recommendedSizes;
        private readonly string _title;
        private ID2D1SolidColorBrush _blackBrush;
        private volatile float _brightness = 1f;
        private volatile int _clientHeight;
        private volatile int _clientWidth;

        // D2D 资源（仅 Letterbox/Center 模式使用）
        private ID2D1DeviceContext _d2dContext;
        private IDCompositionDevice _dcompDevice;
        private IDCompositionTarget _dcompTarget;
        private IDCompositionVisual _dcompVisual;
        private volatile bool _disposed;

        // STA 线程拥有的资源。
        // 通过 DirectComposition 将 SwapChain 内容合成到窗口，
        // 避免 NVIDIA 驱动下的多 SwapChain 串扰问题。
        private StaForm _form;
        private volatile bool _formReady;

        /// 首帧 Present 后已 Show 过（防止重复 Show）
        /// 标记是否已 Show 过（首帧 DoPresent 完成后置 true，此后不再回退）。
        /// 用作 SyncExternalCopy 的判据：未 Show 过时必须 CopyResource 保证首帧内容有效。
        private volatile bool _formShown;

        /// <summary>
        ///     窗口当前是否可见（用户关闭或 Hide 后为 false，Show 后为 true）。
        ///     DoPresent 在此为 false 时跳过 CopyResource/Present，避免向隐藏的 SwapChain 写入。
        /// </summary>
        private volatile bool _formVisible;

        // 帧延迟等待对象（包装 SwapChain 的 waitable handle；ownsHandle=false，句柄归 SwapChain 所有）。
        // Present 前用 WaitOne(0) 非阻塞探测队列状态，避免双缓冲下 Present 阻塞 STA 线程。
        private ManualResetEvent _frameLatencyWaitable;

        // 图形资源是否已释放（ShutdownGraphics 幂等标志）
        private bool _graphicsShutdown;

        /// Detach 在首帧前调用 Hide 的场景，抑制首帧 Show
        private volatile bool _hideRequested;

        // STA 退出信号事件：由 STA 线程创建，并在其 finally 中置零并关闭（谁创建谁释放）。
        // Dispose 仅做快照等待，不参与句柄生命周期管理
        private volatile IntPtr _hStaExitEvent;

        private volatile IntPtr _hwnd = IntPtr.Zero;
        private ID2D1Bitmap1 _intermediateBitmap;
        private ID3D11Texture2D _intermediateTexture;
        private volatile bool _isInSizingLoop;
        private ScreenDisplayMode _mode;
        private volatile bool _needsResize;
        private volatile bool _needsSrcBitmapRecreate;

        // 跳帧挂起标志：共享纹理有内容尚未成功 Present（探测失败置位，成功 Present / 隐藏后清除）
        private volatile bool _presentPending;

        // 常规 present 消息合并标志：防止 STA 线程繁忙时消息队列积压
        private volatile bool _presentQueued;
        private volatile int _renderHeight;
        private Size _renderSize;
        private volatile int _renderWidth;

        // 帧延迟信号的重试注册：DWM 释放 buffer 时回调投递 force-present 消息
        private RegisteredWaitHandle _retryRegistration;
        private volatile bool _running;
        private ID3D11Texture2D _sharedTexture;

        // 重新显示挂起标志：WM_USER_SHOW 置位后窗口暂不显示，
        // 等 DoPresent 把新帧 Present 进 backbuffer 后才真正 Visible=true，
        // 避免外屏窗口先显示 SwapChain 里隐藏前的残留画面
        private volatile bool _showPending;
        private ID2D1Bitmap1 _srcBitmap;
        private volatile bool _staStarted;

        private Thread _staThread;
        private int _staThreadId;
        private IDXGISwapChain1 _swapChain;

        public ExternalDisplayForm(
            MonitorContext context,
            IList<Size> recommendedSizes,
            Size renderSize,
            bool isTopMost,
            IntPtr parentHandle,
            string title,
            ScreenDisplayMode mode
        )
        {
            _context = context;
            _recommendedSizes = recommendedSizes.ToList();
            _renderSize = renderSize;
            _isTopMost = isTopMost;
            _parentHandle = parentHandle;
            _title = title;
            _mode = mode;
            DisplayMode = mode;
        }

        private bool UsesD2D => _mode == ScreenDisplayMode.Letterbox || _mode == ScreenDisplayMode.Center;
        public ScreenDisplayMode DisplayMode { get; set; }
        public bool IsHwndReady => _formReady;
        public bool IsInSizingLoop => _isInSizingLoop;
        public bool IsDisposed => _disposed;
        public bool IsVisible => _formVisible;
        public bool HasShownOnce => _formShown;
        public bool NeedsResize => _needsResize;
        public Size ClientSize => new Size(_clientWidth, _clientHeight);

        public float Brightness
        {
            set => _brightness = Math.Max(0f, Math.Min(1f, value));
        }


        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _running = false;
            // 立即标记不可用
            _formReady = false;
            var hwnd = _hwnd;
            var thread = _staThread;
            var exitEvent = _hStaExitEvent;
            if (hwnd != IntPtr.Zero)
                Win32Helper.PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            else if (_staThreadId != 0 && thread != null && thread.IsAlive)
                // 线程 ID 可能被系统复用，仅在线程确认存活时投递，避免 WM_QUIT 落入无关线程
                Win32Helper.PostThreadMessage(_staThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _staThread = null;
            if (exitEvent == IntPtr.Zero) return;
            // 事件由 STA 线程创建，在 ShutdownGraphics（图形资源释放完毕）时触发、finally 中关闭。
            // 等待语义 = 共享 D3D 设备不再被 STA 触碰，而非线程完全退出：
            // STA 处理 WM_CLOSE 时先 ShutdownGraphics 尽早 SetEvent，其后即使窗口销毁
            // 被 DWM 全屏切换阻塞，也不影响本等待（旧实现等 finally 才 SetEvent，全屏外屏每屏白等 1s）
            if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
                Win32Helper.WaitForSingleObject(exitEvent, 1000);
        }

        public event EventHandler<MouseEventArgs> OnLeftClick;
        public event EventHandler Hidden;

        public void SetSharedTexture(ID3D11Texture2D texture)
        {
            _sharedTexture = texture;
        }

        /// <summary>
        ///     热重载：分辨率变更后同步 renderSize。Stretch 模式需 resize SwapChain，
        ///     Letterbox/Center 模式需重建 _srcBitmap（D2D bitmap 包裹新纹理的 DXGI surface）。
        ///     本方法由 BVE tick 线程调用；_srcBitmap 的实际重建在 STA 线程的 DoPresent 中执行。
        /// </summary>
        public void SetRenderSize(Size newSize)
        {
            _renderSize = newSize;
            if (_mode == ScreenDisplayMode.Stretch)
            {
                _renderWidth = newSize.Width;
                _renderHeight = newSize.Height;
                _needsResize = true;
            }

            _needsSrcBitmapRecreate = true;
            _form?.BeginInvoke(new Action(() => _form.UpdateRenderSize(newSize)));
        }

        /// <summary>
        ///     热重载：DisplayMode 变更。更新 _mode 并触发 SwapChain 重建 + D2D 资源迁移。
        ///     Stretch ↔ Letterbox/Center 切换时 SwapChain 尺寸变化（renderSize ↔ 窗口尺寸），
        ///     通过 _needsResize 在 STA 线程的 DoPresent 中统一处理。
        /// </summary>
        public void SetDisplayMode(ScreenDisplayMode newMode)
        {
            if (_mode == newMode) return;
            _mode = newMode;
            DisplayMode = newMode;

            if (_mode == ScreenDisplayMode.Stretch)
            {
                _renderWidth = _renderSize.Width;
                _renderHeight = _renderSize.Height;
            }
            else
            {
                _renderWidth = _clientWidth;
                _renderHeight = _clientHeight;
            }

            _needsResize = true;
            _needsSrcBitmapRecreate = true;
        }

        public void Start()
        {
            if (_staStarted) return;
            _staStarted = true;
            _running = true;
            _staThread = new Thread(StaThreadProc)
            {
                IsBackground = true,
                Name = $"ExternalDisplaySTA_{_title}"
            };
            _staThreadId = _staThread.ManagedThreadId;
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();
        }

        public void NotifyFrameReady()
        {
            var hwnd = _hwnd;
            if (!_formReady || hwnd == IntPtr.Zero)
                return;
            // 合并重复请求：present 消息只表达“可能有新内容”，呈现时读共享纹理即最新值
            if (_presentQueued) return;
            _presentQueued = true;
            if (!Win32Helper.PostMessage(hwnd, WM_USER_PRESENT, IntPtr.Zero, IntPtr.Zero))
                _presentQueued = false;
        }

        public void Hide()
        {
            _hideRequested = true;
            _formVisible = false;
            _showPending = false;
            var hwnd = _hwnd;
            if (_formReady && hwnd != IntPtr.Zero)
                Win32Helper.PostMessage(hwnd, WM_USER_HIDE, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        ///     重新显示已隐藏的窗口（用户关闭或 Hide 后调用）。
        ///     清除 _hideRequested 以允许 DoPresent 首帧 Show，并投递 WM_USER_SHOW 通知 STA 线程恢复可见。
        ///     实际显示延迟到 DoPresent 成功 Present 新帧之后（_showPending），避免显示隐藏前的残留。
        /// </summary>
        public void Show()
        {
            if (_disposed) return;
            // 状态标志必须在 _formReady 检查之前设置，
            // 确保延迟初始化时渲染管线能感知到可见性变更
            _hideRequested = false;
            _formVisible = true;
            if (!_formReady) return;
            var hwnd = _hwnd;
            if (hwnd != IntPtr.Zero)
                Win32Helper.PostMessage(hwnd, WM_USER_SHOW, IntPtr.Zero, IntPtr.Zero);
        }

        private void StaThreadProc()
        {
            // 退出事件由本线程创建并在 finally 中关闭（谁创建谁释放）；Dispose 只做快照等待
            var exitEvent = Win32Helper.CreateEvent(IntPtr.Zero, true, false, null);
            _hStaExitEvent = exitEvent;
            try
            {
                if (!_running) return;
                _form = new StaForm(this, _recommendedSizes, _renderSize, _isTopMost, _parentHandle, _title, _mode);
                _hwnd = _form.Handle;
                if (_parentHandle != IntPtr.Zero)
                    Win32Helper.SetWindowLongPtr(_hwnd, Win32Helper.GWLP_HWNDPARENT, _parentHandle);
                _form.GetPhysicalClientSize(out var initW, out var initH);
                UpdateClientSizeCache();
                if (!_running) return;
                if (UsesD2D)
                {
                    _renderWidth = initW;
                    _renderHeight = initH;
                }
                else
                {
                    _renderWidth = _renderSize.Width;
                    _renderHeight = _renderSize.Height;
                }

                CreateD2DResources();
                CreateSwapChain();
                CreateIntermediateResources();
                UpdateVisualTransform();
                ClearBackBuffer();
                if (!_running) return;
                _formReady = true;
                Application.Run();
            }
            finally
            {
                // CleanupResources 内部先 ShutdownGraphics：释放图形资源并 SetEvent（若尚未触发），
                // 之后才销毁窗体，不得拖住 Dispose 的等待。
                CleanupResources();
                if (exitEvent != IntPtr.Zero)
                {
                    // 先清零再关闭：句柄生命周期归本线程所有（谁创建谁关闭）
                    _hStaExitEvent = IntPtr.Zero;
                    Win32Helper.CloseHandle(exitEvent);
                }
            }
        }

        private void UpdateClientSizeCache()
        {
            var form = _form;
            if (form == null) return;
            var sz = form.ClientSize;
            _clientWidth = sz.Width;
            _clientHeight = sz.Height;
        }

        private void OnFormResize()
        {
            var form = _form;
            if (form == null) return;
            UpdateClientSizeCache();

            if (_mode == ScreenDisplayMode.Stretch)
            {
                // 无需 resize SwapChain
                UpdateVisualTransform();
                return;
            }

            // Letterbox/Center: 拖拽调整大小时不 resize SwapChain，
            // 等 WM_EXITSIZEMOVE 在 OnExitSizeMove 统一处理，
            // 但非拖拽调整大小时触发此路径
            if (_isInSizingLoop) return;
            form.GetPhysicalClientSize(out var newW, out var newH);
            if (newW > 0 && newH > 0 && (newW != _renderWidth || newH != _renderHeight))
            {
                _renderWidth = newW;
                _renderHeight = newH;
                _needsResize = true;
            }
        }

        private void OnExitSizeMove()
        {
            _isInSizingLoop = false;
            var form = _form;
            if (form == null) return;
            UpdateClientSizeCache();

            if (_mode == ScreenDisplayMode.Stretch)
            {
                UpdateVisualTransform();
                return;
            }

            form.GetPhysicalClientSize(out var newW, out var newH);
            if (newW > 0 && newH > 0 && (newW != _renderWidth || newH != _renderHeight))
            {
                _renderWidth = newW;
                _renderHeight = newH;
                _needsResize = true;
            }
        }

        /// <summary>
        ///     更新 DComp visual 的 transform：
        ///     - Stretch: 缩放 SwapChain（原始尺寸）到窗口物理客户区尺寸
        ///     - Letterbox/Center: identity（SwapChain 已匹配窗口尺寸）
        /// </summary>
        private void UpdateVisualTransform()
        {
            if (_dcompVisual == null || _dcompDevice == null || _form == null) return;

            try
            {
                if (_mode == ScreenDisplayMode.Stretch)
                {
                    _form.GetPhysicalClientSize(out var physW, out var physH);
                    var scaleX = (float)physW / Math.Max(1, _renderSize.Width);
                    var scaleY = (float)physH / Math.Max(1, _renderSize.Height);
                    _dcompVisual.SetTransform(Matrix3x2.CreateScale(scaleX, scaleY));
                }
                else
                {
                    _dcompVisual.SetTransform(Matrix3x2.Identity);
                }

                _dcompDevice.Commit();
            }
            catch (Exception ex)
            {
                _context.Debugger?.AddLineLasting($"[ExternalDisplayForm] UpdateVisualTransform failed: {ex.Message}");
            }
        }

        private void CreateSwapChain()
        {
            try
            {
                using (var factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>())
                {
                    var desc = new SwapChainDescription1
                    {
                        Width = Math.Max(1, _renderWidth),
                        Height = Math.Max(1, _renderHeight),
                        Format = Format.B8G8R8A8_UNorm,
                        BufferUsage = Usage.RenderTargetOutput,
                        BufferCount = 2,
                        SwapEffect = SwapEffect.FlipDiscard,
                        SampleDescription = new SampleDescription(1, 0),
                        Scaling = Scaling.Stretch,
                        AlphaMode = AlphaMode.Ignore,
                        // 帧延迟等待对象：双缓冲下 DWM 持有一个 buffer，无此标志时 Present 会阻塞等待 vblank
                        Flags = SwapChainFlags.FrameLatencyWaitableObject
                    };
                    // SwapChain 不直接绑定 HWND，由 DComp visual 合成到窗口
                    _swapChain = factory.CreateSwapChainForComposition(_context.D3D11Device, desc);
                    AcquireFrameLatencyWaitable();
                    // 首次创建时初始化 DComp 设备/目标/visual
                    if (_dcompDevice == null)
                    {
                        SetupDComp();
                    }
                    else
                    {
                        // 重建 SwapChain 时重新绑定到现有 visual
                        _dcompVisual.SetContent(_swapChain);
                        _dcompDevice.Commit();
                    }

                    _context.Debugger?.AddLineLasting(
                        $"[ExternalDisplayForm] SwapChain+DComp created. Size={_renderWidth}x{_renderHeight}");
                }
            }
            catch (Exception ex)
            {
                _context.Debugger?.AddLineLasting($"[ExternalDisplayForm] Failed to create SwapChain: {ex.Message}");
            }
        }

        /// <summary>
        ///     创建 DComp 设备、target 和 visual，将 SwapChain 绑定为窗口内容。
        ///     DComp 合成可避免 NVIDIA 驱动下多 SwapChain 直接 Present 到窗口导致的串扰。
        /// </summary>
        private void SetupDComp()
        {
            _dcompDevice = DComp.DCompositionCreateDevice<IDCompositionDevice>(_context.DxgiDevice);
            _dcompDevice.CreateTargetForHwnd(_form.Handle, true, out _dcompTarget);
            _dcompDevice.CreateVisual(out _dcompVisual);
            _dcompVisual.SetContent(_swapChain);
            _dcompTarget.SetRoot(_dcompVisual);
            _dcompDevice.Commit();
        }

        private void RebuildSwapChain()
        {
            try
            {
                ClearPendingRetry();
                // 先释放旧 waitable（句柄归旧 SwapChain 所有），避免重建失败时残留失效句柄的包装器
                _frameLatencyWaitable?.Dispose();
                _frameLatencyWaitable = null;
                _context.D3D11Context?.Flush();
                _swapChain?.Dispose();
                _swapChain = null;
                CreateSwapChain();
            }
            catch (Exception ex)
            {
                _context.Debugger?.AddLineLasting($"[ExternalDisplayForm] Failed to rebuild SwapChain: {ex.Message}");
            }
        }

        private void ResizeSwapChain()
        {
            if (_swapChain == null)
            {
                RebuildSwapChain();
                return;
            }

            _context.D3D11Context?.Flush();
            // FrameLatencyWaitableObject 标志必须在 ResizeBuffers 时与创建时保持一致，否则 DXGI 报错
            _swapChain.ResizeBuffers(0, Math.Max(1, _renderWidth), Math.Max(1, _renderHeight),
                Format.B8G8R8A8_UNorm, SwapChainFlags.FrameLatencyWaitableObject);
            ClearBackBuffer();
        }

        private void ClearBackBuffer()
        {
            if (_swapChain == null) return;
            var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            using (backBuffer)
            {
                var rtv = _context.D3D11Device.CreateRenderTargetView(backBuffer);
                using (rtv)
                {
                    _context.D3D11Context.ClearRenderTargetView(rtv, new Color4(0, 0, 0, 1));
                }
            }
        }

        // 在 STA 线程的 WndProc 中调用
        // Stretch: 直接 CopyResource(backBuffer, sharedTexture)，DComp visual transform 拉伸
        // Letterbox/Center: D2D 渲染到中间纹理 → CopyResource(backBuffer, intermediate)
        // force = 帧延迟信号驱动的重试路径：跳过探测直接 Present
        //   （waitable object 为 auto-reset 语义，注册等待已消耗信号，重试时再探测必然失败）
        private void DoPresent(bool force)
        {
            // 常规消息已出队，允许 NotifyFrameReady 投递下一条（force 消息本就未置位）
            _presentQueued = false;

            if (_swapChain == null || _sharedTexture == null)
            {
                ClearPendingRetry();
                return;
            }

            if (_needsResize)
            {
                _needsResize = false;
                DisposeIntermediateResources();
                ResizeSwapChain();
                CreateIntermediateResources();
                UpdateVisualTransform();

                if (_swapChain == null)
                {
                    ClearPendingRetry();
                    return;
                }
            }

            if (_needsSrcBitmapRecreate && _sharedTexture != null)
            {
                _needsSrcBitmapRecreate = false;
                _srcBitmap?.Dispose();
                _srcBitmap = null;
                using (var dxgiSurface = _sharedTexture.QueryInterface<IDXGISurface>())
                {
                    _srcBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurface,
                        RenderHelper.BitMapProperties8Bit);
                }
            }

            if (_formShown && !_formVisible)
            {
                // 隐藏时 SwapChain 保留最后一帧，无需补 present；同时撤销未完成的挂起显示
                _showPending = false;
                ClearPendingRetry();
                return;
            }

            // 陈旧的 force 消息：挂起的内容已通过常规路径呈现
            if (force && !_presentPending)
                return;

            // 非阻塞探测帧延迟：队列满（外屏未消费上一帧）时挂起而非阻塞，
            // 由信号回调在 DWM 释放 buffer 的瞬间重试；呈现时读共享纹理即最新内容
            if (!force && _frameLatencyWaitable != null && !_frameLatencyWaitable.WaitOne(0))
            {
                _presentPending = true;
                SchedulePendingRetry();
                return;
            }

            ClearPendingRetry();

            var needsD2D = UsesD2D || _brightness < 1f;

            if (needsD2D && _d2dContext != null && _intermediateBitmap != null)
            {
                _d2dContext.Target = _intermediateBitmap;
                _d2dContext.BeginDraw();
                _d2dContext.Clear(new Color4(0, 0, 0, 1));

                var srcW = _renderSize.Width;
                var srcH = _renderSize.Height;
                var dstW = _renderWidth;
                var dstH = _renderHeight;

                if (_mode == ScreenDisplayMode.Letterbox)
                {
                    var scale = Math.Min((float)dstW / srcW, (float)dstH / srcH);
                    var newW = srcW * scale;
                    var newH = srcH * scale;
                    var x = (dstW - newW) / 2f;
                    var y = (dstH - newH) / 2f;
                    _d2dContext.DrawBitmap(_srcBitmap, new RectangleF(x, y, newW, newH), 1f,
                        BitmapInterpolationMode.Linear, null);
                }
                else if (_mode == ScreenDisplayMode.Center)
                {
                    var x = (dstW - srcW) / 2f;
                    var y = (dstH - srcH) / 2f;
                    _d2dContext.DrawBitmap(_srcBitmap, new RectangleF(x, y, srcW, srcH), 1f,
                        BitmapInterpolationMode.NearestNeighbor, null);
                }
                else
                {
                    _d2dContext.DrawBitmap(_srcBitmap,
                        new RectangleF(0, 0, dstW, dstH), 1f,
                        BitmapInterpolationMode.Linear, null);
                }

                if (_brightness < 1f)
                {
                    _blackBrush.Color = new Color4(0, 0, 0, 1f - _brightness);
                    _d2dContext.FillRectangle(
                        new RectangleF(0, 0, dstW, dstH), _blackBrush);
                }

                _d2dContext.EndDraw();
                _d2dContext.Target = null;

                var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
                using (backBuffer)
                {
                    _context.D3D11Context.CopyResource(backBuffer, _intermediateTexture);
                    _swapChain.Present(0, PresentFlags.None);
                }
            }
            else
            {
                var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
                using (backBuffer)
                {
                    _context.D3D11Context.CopyResource(backBuffer, _sharedTexture);
                    _swapChain.Present(0, PresentFlags.None);
                }
            }

            if (!_formShown && !_hideRequested)
            {
                _formShown = true;
                _formVisible = true;
                _showPending = false;
                _form.Show();
            }
            else if (_showPending)
            {
                // 重新显示：新帧已 Present 进 backbuffer，此时才真正显示窗口
                _showPending = false;
                _form.Visible = true;
            }
        }

        /// <summary>
        ///     获取帧延迟等待对象（MaximumFrameLatency=1）并包装为 WaitHandle。
        ///     句柄归 SwapChain 所有（ownsHandle=false）；SwapChain 重建（CreateSwapChain）后必须重新调用。
        ///     失败时降级为旧行为（探测跳过，Present 可能阻塞）。
        /// </summary>
        private void AcquireFrameLatencyWaitable()
        {
            ClearPendingRetry();
            _frameLatencyWaitable?.Dispose();
            _frameLatencyWaitable = null;
            ManualResetEvent waitable = null;
            try
            {
                using (var swapChain2 = _swapChain.QueryInterface<IDXGISwapChain2>())
                {
                    swapChain2.MaximumFrameLatency = 1;
                    waitable = new ManualResetEvent(false);
                    // 用 DXGI 的 waitable handle 替换构造器创建的临时句柄
                    var tempHandle = waitable.SafeWaitHandle;
                    waitable.SafeWaitHandle = new SafeWaitHandle(swapChain2.FrameLatencyWaitableObject, false);
                    tempHandle.Dispose();
                }

                _frameLatencyWaitable = waitable;
                waitable = null;
            }
            catch (Exception ex)
            {
                // 失败路径：释放未交付的临时事件句柄（SafeWaitHandle 已替换时 ownsHandle=false，Dispose 不会误关 DXGI 句柄）
                waitable?.Dispose();
                _context.Debugger?.AddLineLasting(
                    $"[ExternalDisplayForm] AcquireFrameLatencyWaitable failed: {ex.Message}");
            }
        }

        /// <summary>
        ///     清除跳帧挂起状态并注销重试注册。
        ///     Unregister 至多等待进行中的回调完成（回调仅 PostMessage，微秒级），不会死锁 STA。
        /// </summary>
        private void ClearPendingRetry()
        {
            _presentPending = false;
            var retry = _retryRegistration;
            _retryRegistration = null;
            retry?.Unregister(null);
        }

        /// <summary>
        ///     注册帧延迟信号等待：DWM 释放 buffer（vblank）时投递 force-present 消息。
        ///     回调运行在线程池，只允许 PostMessage，不得触碰 D3D/D2D/窗体资源。
        /// </summary>
        private void SchedulePendingRetry()
        {
            _retryRegistration?.Unregister(null);
            _retryRegistration = ThreadPool.RegisterWaitForSingleObject(
                _frameLatencyWaitable,
                (state, timedOut) =>
                {
                    var hwnd = _hwnd;
                    if (hwnd != IntPtr.Zero)
                        Win32Helper.PostMessage(hwnd, WM_USER_PRESENT, (IntPtr)1, IntPtr.Zero);
                },
                null, Timeout.Infinite, true);
        }

        private void CreateD2DResources()
        {
            _d2dContext = _context.D2D1Device.CreateDeviceContext();
            _d2dContext.Dpi = new SizeF(96, 96);
            using (var dxgiSurface = _sharedTexture.QueryInterface<IDXGISurface>())
            {
                _srcBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurface, RenderHelper.BitMapProperties8Bit);
            }

            _blackBrush = _d2dContext.CreateSolidColorBrush(new Color4(0, 0, 0, 0));
        }

        private void CreateIntermediateResources()
        {
            _intermediateTexture = _context.D3D11Device.CreateTexture2D(
                RenderHelper.CreateRenderTargetTextureDescription(Math.Max(1, _renderWidth),
                    Math.Max(1, _renderHeight)));
            using (var dxgiSurface = _intermediateTexture.QueryInterface<IDXGISurface>())
            {
                _intermediateBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurface,
                    RenderHelper.BitMapProperties8Bit);
            }
        }

        private void DisposeIntermediateResources()
        {
            _d2dContext.Target = null;
            _intermediateBitmap?.Dispose();
            _intermediateBitmap = null;
            _intermediateTexture?.Dispose();
            _intermediateTexture = null;
        }

        /// <summary>
        ///     关闭路径：立即释放全部图形资源（D2D/DComp/SwapChain）并触发退出事件。
        ///     Dispose 等待的语义只是"共享 D3D 设备不再被本线程触碰"，与窗口销毁无关；
        ///     窗口销毁可能因全屏切换长时间阻塞，必须推迟到事件触发之后。
        ///     从 OnFormClosing（_running=false 分支）与 StaThreadProc.finally 双路径调用，幂等。
        /// </summary>
        private void ShutdownGraphics()
        {
            if (_graphicsShutdown) return;
            _graphicsShutdown = true;
            _formReady = false;
            ClearPendingRetry();
            _frameLatencyWaitable?.Dispose();
            _frameLatencyWaitable = null;
            _intermediateBitmap?.Dispose();
            _intermediateBitmap = null;
            _intermediateTexture?.Dispose();
            _intermediateTexture = null;
            _srcBitmap?.Dispose();
            _srcBitmap = null;
            _blackBrush?.Dispose();
            _blackBrush = null;
            _d2dContext?.Dispose();
            _d2dContext = null;
            _dcompVisual?.Dispose();
            _dcompVisual = null;
            _dcompTarget?.Dispose();
            _dcompTarget = null;
            _dcompDevice?.Dispose();
            _dcompDevice = null;
            _swapChain?.Dispose();
            _swapChain = null;
            _sharedTexture = null;
            var exitEvent = _hStaExitEvent;
            if (exitEvent != IntPtr.Zero)
                Win32Helper.SetEvent(exitEvent);
        }

        private void CleanupResources()
        {
            ShutdownGraphics();
            try
            {
                _form?.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }

            _form = null;
            _hwnd = IntPtr.Zero;
        }


        public static Vector2? MapClickPosition(ScreenDisplayMode mode,
            float clickX, float clickY,
            int clientW, int clientH, int srcW, int srcH)
        {
            float mappedX, mappedY;
            switch (mode)
            {
                case ScreenDisplayMode.Stretch:
                    mappedX = clickX * srcW / clientW;
                    mappedY = clickY * srcH / clientH;
                    break;
                case ScreenDisplayMode.Center:
                    mappedX = clickX - (clientW - srcW) / 2f;
                    mappedY = clickY - (clientH - srcH) / 2f;
                    break;
                case ScreenDisplayMode.Letterbox:
                default:
                    var scaleX = (float)clientW / srcW;
                    var scaleY = (float)clientH / srcH;
                    var scale = Math.Min(scaleX, scaleY);
                    var newW = srcW * scale;
                    var newH = srcH * scale;
                    var lx = (clientW - newW) / 2f;
                    var ly = (clientH - newH) / 2f;
                    mappedX = (clickX - lx) / scale;
                    mappedY = (clickY - ly) / scale;
                    break;
            }

            if (mappedX >= 0 && mappedX <= srcW && mappedY >= 0 && mappedY <= srcH)
                return new Vector2(mappedX, mappedY);
            return null;
        }

        private class StaForm : Form
        {
            private const int BLACK_BRUSH = 4;
            private readonly ToolStripMenuItem _fitHeightItem;
            private readonly ToolStripMenuItem _fitWidthItem;
            private readonly ToolStripMenuItem _fullScreenItem;
            private readonly ExternalDisplayForm _owner;
            private readonly IntPtr _parentHandle;

            private readonly List<(Size size, ToolStripMenuItem item)> _sizeItems =
                new List<(Size size, ToolStripMenuItem item)>();

            private ContextMenuStrip _contextMenu;
            private bool _isFullScreen;
            private Size _renderSize;
            private Rectangle _savedBounds;
            private FormBorderStyle _savedFormBorderStyle;
            private bool _savedTopMost;

            public StaForm(
                ExternalDisplayForm owner,
                List<Size> recommendedSizes,
                Size renderSize,
                bool isTopMost,
                IntPtr parentHandle,
                string title,
                ScreenDisplayMode mode
            )
            {
                _owner = owner;
                _parentHandle = parentHandle;
                _renderSize = renderSize;
                Text = title;
                ClientSize = renderSize;
                BackColor = Color.Black;
                StartPosition = FormStartPosition.CenterScreen;
                _owner.DisplayMode = mode;
                DoubleBuffered = false;
                TopMost = isTopMost;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
                _contextMenu = new ContextMenuStrip();

                foreach (var size in recommendedSizes)
                {
                    var item = new ToolStripMenuItem(MakeSizeItemText(size, _renderSize));
                    item.Click += (s, e) =>
                    {
                        if (_isFullScreen) ToggleFullScreen();
                        ClientSize = size;
                    };
                    _contextMenu.Items.Add(item);
                    _sizeItems.Add((size, item));
                }

                _contextMenu.Items.Add(new ToolStripSeparator());

                _fitWidthItem = new ToolStripMenuItem("Fit Aspect Ratio to Width");
                _fitWidthItem.Click += (s, e) =>
                {
                    if (_isFullScreen) return;
                    if (_renderSize.Width <= 0 || _renderSize.Height <= 0) return;
                    var currentWidth = ClientSize.Width;
                    var targetHeight = (int)Math.Round((float)currentWidth * _renderSize.Height / _renderSize.Width,
                        MidpointRounding.AwayFromZero);
                    ClientSize = new Size(currentWidth, Math.Max(1, targetHeight));
                };
                _contextMenu.Items.Add(_fitWidthItem);

                _fitHeightItem = new ToolStripMenuItem("Fit Aspect Ratio to Height");
                _fitHeightItem.Click += (s, e) =>
                {
                    if (_isFullScreen) return;
                    if (_renderSize.Width <= 0 || _renderSize.Height <= 0) return;
                    var currentHeight = ClientSize.Height;
                    var targetWidth = (int)Math.Round((float)currentHeight * _renderSize.Width / _renderSize.Height,
                        MidpointRounding.AwayFromZero);
                    ClientSize = new Size(Math.Max(1, targetWidth), currentHeight);
                };
                _contextMenu.Items.Add(_fitHeightItem);

                _contextMenu.Items.Add(new ToolStripSeparator());
                _fullScreenItem = new ToolStripMenuItem("FullScreen");
                _fullScreenItem.Click += (s, e) => ToggleFullScreen();
                _contextMenu.Items.Add(_fullScreenItem);

                UpdateSizeMenuCheckState();
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE;
                    return cp;
                }
            }

            public void ToggleFullScreen()
            {
                if (!_isFullScreen)
                {
                    _savedBounds = Bounds;
                    _savedFormBorderStyle = FormBorderStyle;
                    _savedTopMost = TopMost;
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(Handle);
                    FormBorderStyle = FormBorderStyle.None;
                    TopMost = true;
                    Bounds = currentScreen.Bounds;
                    _isFullScreen = true;
                }
                else
                {
                    FormBorderStyle = _savedFormBorderStyle;
                    TopMost = _savedTopMost;
                    Bounds = _savedBounds;
                    _isFullScreen = false;
                }

                UpdateSizeMenuCheckState();
            }

            private static string MakeSizeItemText(Size size, Size renderSize)
            {
                var text = $"{size.Width}x{size.Height}";
                if (size == renderSize) text += " (Render size)";
                return text;
            }

            public void UpdateRenderSize(Size newRenderSize)
            {
                if (_renderSize == newRenderSize) return;
                _renderSize = newRenderSize;
                foreach (var (size, item) in _sizeItems) item.Text = MakeSizeItemText(size, _renderSize);

                UpdateSizeMenuCheckState();
            }

            public void GetPhysicalClientSize(out int width, out int height)
            {
                float dpiScale;
                try
                {
                    dpiScale = DeviceDpi / 96f;
                }
                catch (Exception)
                {
                    using (var graphics = CreateGraphics())
                    {
                        dpiScale = graphics.DpiX / 96f;
                    }
                }

                width = (int)Math.Round(ClientSize.Width * dpiScale, MidpointRounding.AwayFromZero);
                height = (int)Math.Round(ClientSize.Height * dpiScale, MidpointRounding.AwayFromZero);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                _owner.OnFormResize();
                UpdateSizeMenuCheckState();
            }

            private void UpdateSizeMenuCheckState()
            {
                if (_fullScreenItem != null) _fullScreenItem.Checked = _isFullScreen;

                if (_fitWidthItem != null) _fitWidthItem.Enabled = !_isFullScreen;

                if (_fitHeightItem != null) _fitHeightItem.Enabled = !_isFullScreen;

                var currentSize = ClientSize;
                foreach (var (size, item) in _sizeItems) item.Checked = !_isFullScreen && currentSize == size;
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_ERASEBKGND:
                        if (m.WParam != IntPtr.Zero)
                        {
                            Win32Helper.GetClientRect(m.HWnd, out var rect);
                            Win32Helper.FillRect(m.WParam, ref rect, Win32Helper.GetStockObject(BLACK_BRUSH));
                        }

                        m.Result = (IntPtr)1;
                        return;
                    case WM_PAINT:
                        DefWndProc(ref m);
                        return;
                    case WM_USER_PRESENT:
                        // wParam=1：帧延迟信号重试路径（force，跳过探测）
                        _owner.DoPresent(m.WParam != IntPtr.Zero);
                        m.Result = IntPtr.Zero;
                        return;
                    case WM_USER_HIDE:
                        // 隐藏路径不做任何窗口样式操作（全屏还原移至 WM_USER_SHOW）：
                        // 关闭线路时本消息先于 WM_CLOSE 入队，若在此还原全屏样式，
                        // SetWindowPos 触发的 DWM 全屏切换会阻塞 STA，拖住 Dispose 的等待
                        _owner._formVisible = false;
                        _owner._showPending = false;
                        Visible = false;
                        m.Result = IntPtr.Zero;
                        return;
                    case WM_USER_SHOW:
                        // 重新显示时把隐藏期间保留的全屏样式还原为普通窗口
                        // （终态与旧行为一致：hide→show 后以窗口模式呈现）
                        if (_isFullScreen) ToggleFullScreen();
                        _owner._formVisible = true;
                        _owner._hideRequested = false;
                        // 不立即 Visible=true：先挂起，等 DoPresent 把新帧 Present 进
                        // backbuffer 后再显示，避免窗口先亮出隐藏前的残留画面
                        _owner._showPending = true;
                        m.Result = IntPtr.Zero;
                        return;
                    case WM_ENTERSIZEMOVE:
                        _owner._isInSizingLoop = true;
                        break;
                    case WM_EXITSIZEMOVE:
                        _owner.OnExitSizeMove();
                        break;
                    case WM_MOUSEACTIVATE:
                        m.Result = (IntPtr)MA_NOACTIVATE;
                        return;
                }

                base.WndProc(ref m);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (_parentHandle != IntPtr.Zero)
                    Win32Helper.PostMessage(_parentHandle, Win32Helper.WM_KEYDOWN, (IntPtr)e.KeyCode, IntPtr.Zero);
                base.OnKeyDown(e);
            }

            protected override void OnKeyUp(KeyEventArgs e)
            {
                if (_parentHandle != IntPtr.Zero)
                    Win32Helper.PostMessage(_parentHandle, Win32Helper.WM_KEYUP, (IntPtr)e.KeyCode, IntPtr.Zero);
                base.OnKeyUp(e);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button == MouseButtons.Left)
                    _owner.OnLeftClick?.Invoke(this, e);
                else if (e.Button == MouseButtons.Right)
                    _contextMenu?.Show(this, e.Location);
            }

            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                if (!_owner._running)
                {
                    // 插件关闭路径：先释放图形资源并触发退出事件，再走窗口销毁。
                    // 窗口销毁可能因 DWM 全屏切换长时间阻塞，不得让它拖住 Dispose 的等待
                    _owner.ShutdownGraphics();
                    base.OnFormClosing(e);
                    return;
                }

                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    _owner._formVisible = false;
                    if (_isFullScreen) ToggleFullScreen();
                    Visible = false;
                    _owner.Hidden?.Invoke(_owner, EventArgs.Empty);
                    return;
                }

                base.OnFormClosing(e);
            }

            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                base.OnFormClosed(e);
                Application.ExitThread();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _contextMenu?.Dispose();
                    _contextMenu = null;
                }

                base.Dispose(disposing);
            }
        }
    }
}