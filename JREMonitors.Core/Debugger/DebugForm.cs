using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JREMonitors.Core.Utils;

namespace JREMonitors.Core.Debugger
{
    /// <summary>
    ///     日志调试窗口。
    /// </summary>
    public class DebugForm : Form, IDebugger
    {
        private const int MaxLastingLines = 200;
        private const int LeftTextBoxUpdateIntervalMs = 33;
        private const string SectionSeparator = "===";
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private readonly Dictionary<string, StringBuilder> _buffers = new Dictionary<string, StringBuilder>();
        private readonly StringBuilder _defaultBuffer = new StringBuilder();
        private readonly Queue<string> _lastingLines = new Queue<string>();
        private readonly TextBox _leftTextBox;
        private readonly object _lockObj = new object();
        private readonly IntPtr _parentHandle;
        private readonly TextBox _rightTextBox;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly List<string> _types;
        private string _currentType;
        private volatile bool _forceClose;
        private volatile bool _isLeftUpdatePending;
        private volatile bool _isRightUpdatePending;
        private long _lastUpdateMs;
        private bool _rightDirty;

        private DebugForm(string[] types, IntPtr parentHandle)
        {
            Text = "JREMonitors Debug Console";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(50, 50);
            TopMost = true;
            _parentHandle = parentHandle;
            _types = types?.ToList() ?? new List<string>();
            foreach (var type in _types)
                _buffers[type] = new StringBuilder();
            _leftTextBox = CreateTextBox();
            _rightTextBox = CreateTextBox();
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 500,
                SplitterWidth = 6,
                BackColor = Color.DimGray,
                IsSplitterFixed = false
            };
            splitContainer.Panel1.Controls.Add(_leftTextBox);
            splitContainer.Panel2.Controls.Add(_rightTextBox);
            Controls.Add(splitContainer);
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

        public void Add(string text)
        {
            lock (_lockObj)
            {
                GetBuffer(_currentType).Append(text);
            }
        }

        public void AddLine(string text)
        {
            lock (_lockObj)
            {
                GetBuffer(_currentType).AppendLine(text);
            }
        }

        public void AddLineLasting(string text)
        {
            lock (_lockObj)
            {
                _lastingLines.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] {text}");
                while (_lastingLines.Count > MaxLastingLines) _lastingLines.Dequeue();
                _rightDirty = true;
            }
        }

        public void ClearRight()
        {
            lock (_lockObj)
            {
                _lastingLines.Clear();
                _rightDirty = true;
            }

            UpdateRightTextBox();
        }

        public event Action Hidden;

        public static DebugForm Start(string[] types = null, IntPtr parentHandle = default)
        {
            DebugForm form = null;
            using (var ready = new ManualResetEventSlim(false))
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        form = new DebugForm(types, parentHandle);
                        _ = form.Handle;
                    }
                    finally
                    {
                        ready.Set();
                    }

                    Application.Run();
                })
                {
                    IsBackground = true,
                    Name = "DebugFormSTA"
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                ready.Wait();
            }

            return form;
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 8F),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnsureParent();
        }

        private void EnsureParent()
        {
            if (_parentHandle != IntPtr.Zero)
                Win32Helper.SetWindowLongPtr(Handle, Win32Helper.GWLP_HWNDPARENT, _parentHandle);
        }

        private void BringToTop()
        {
            if (Handle == IntPtr.Zero) return;
            Win32Helper.SetWindowPos(Handle, Win32Helper.HWND_TOPMOST, 0, 0, 0, 0,
                Win32Helper.SWP_NOMOVE | Win32Helper.SWP_NOSIZE | Win32Helper.SWP_NOACTIVATE);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }

        public void SetType(string type)
        {
            _currentType = type;
        }

        private StringBuilder GetBuffer(string type)
        {
            if (string.IsNullOrEmpty(type))
                return _defaultBuffer;
            if (!_buffers.TryGetValue(type, out var buffer))
            {
                buffer = new StringBuilder();
                _buffers[type] = buffer;
            }

            return buffer;
        }

        public void ClearLeft(string type)
        {
            lock (_lockObj)
            {
                GetBuffer(type).Clear();
            }
        }

        public void ClearLeftDefault()
        {
            lock (_lockObj)
            {
                _defaultBuffer.Clear();
            }
        }

        public void ClearLeftAll()
        {
            lock (_lockObj)
            {
                _defaultBuffer.Clear();
                foreach (var buffer in _buffers.Values)
                    buffer.Clear();
            }
        }

        public void Commit()
        {
            string textToDisplay;
            bool shouldUpdateRight;
            bool intervalElapsed;
            var currentMs = _stopwatch.ElapsedMilliseconds;
            lock (_lockObj)
            {
                textToDisplay = ComposeLeftText();
                intervalElapsed = currentMs - _lastUpdateMs >= LeftTextBoxUpdateIntervalMs;
                if (intervalElapsed)
                {
                    _lastUpdateMs = currentMs;
                    shouldUpdateRight = _rightDirty;
                    _rightDirty = false;
                }
                else
                {
                    shouldUpdateRight = false;
                }
            }

            if (!intervalElapsed) return;

            if (!_isLeftUpdatePending)
            {
                _isLeftUpdatePending = true;
                BeginInvoke(new Action(() =>
                {
                    _isLeftUpdatePending = false;
                    UpdateLeftTextBox(textToDisplay);
                }));
            }

            if (shouldUpdateRight && !_isRightUpdatePending)
            {
                _isRightUpdatePending = true;
                BeginInvoke(new Action(() =>
                {
                    _isRightUpdatePending = false;
                    UpdateRightTextBox();
                }));
            }
        }

        private string ComposeLeftText()
        {
            var sections = new List<string>();
            if (_defaultBuffer.Length > 0)
                sections.Add(_defaultBuffer.ToString().TrimEnd('\r', '\n'));
            foreach (var type in _types)
                if (_buffers.TryGetValue(type, out var buffer) && buffer.Length > 0)
                    sections.Add($"{SectionSeparator}{type}{SectionSeparator}{Environment.NewLine}" +
                                 buffer.ToString().TrimEnd('\r', '\n'));

            return string.Join(Environment.NewLine, sections);
        }

        private void UpdateLeftTextBox(string text)
        {
            if (_leftTextBox.IsDisposed) return;

            if (_leftTextBox.Focused && (MouseButtons & MouseButtons.Left) != 0) return;

            if (_leftTextBox.Text == text) return;

            var hWnd = _leftTextBox.Handle;
            var firstVisibleLine =
                (int)Win32Helper.SendMessage(hWnd, Win32Helper.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            var selStart = _leftTextBox.SelectionStart;
            var selLength = _leftTextBox.SelectionLength;

            Win32Helper.SendMessage(hWnd, Win32Helper.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            _leftTextBox.Text = text;

            if (selStart > 0 || selLength > 0)
                Win32Helper.SendMessage(hWnd, Win32Helper.EM_SETSEL, (IntPtr)selStart, (IntPtr)(selStart + selLength));

            var newFirstVisibleLine =
                (int)Win32Helper.SendMessage(hWnd, Win32Helper.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            var delta = firstVisibleLine - newFirstVisibleLine;
            if (delta != 0) Win32Helper.SendMessage(hWnd, Win32Helper.EM_LINESCROLL, IntPtr.Zero, (IntPtr)delta);

            Win32Helper.SendMessage(hWnd, Win32Helper.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            _leftTextBox.Invalidate();
        }

        private void UpdateRightTextBox()
        {
            if (_rightTextBox.IsDisposed) return;

            if (_rightTextBox.Focused && (MouseButtons & MouseButtons.Left) != 0) return;

            string text;
            lock (_lockObj)
            {
                text = string.Join(Environment.NewLine, _lastingLines);
            }

            if (_rightTextBox.Text == text) return;

            var hWnd = _rightTextBox.Handle;
            var wasAtBottom = IsScrolledToBottom(hWnd);
            var firstVisibleLine =
                (int)Win32Helper.SendMessage(hWnd, Win32Helper.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);

            Win32Helper.SendMessage(hWnd, Win32Helper.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            _rightTextBox.Text = text;

            if (wasAtBottom)
            {
                Win32Helper.SendMessage(hWnd, Win32Helper.WM_VSCROLL, (IntPtr)Win32Helper.SB_BOTTOM, IntPtr.Zero);
            }
            else
            {
                var newFirstVisibleLine = (int)Win32Helper.SendMessage(hWnd, Win32Helper.EM_GETFIRSTVISIBLELINE,
                    IntPtr.Zero, IntPtr.Zero);
                var delta = firstVisibleLine - newFirstVisibleLine;
                if (delta != 0) Win32Helper.SendMessage(hWnd, Win32Helper.EM_LINESCROLL, IntPtr.Zero, (IntPtr)delta);
            }

            Win32Helper.SendMessage(hWnd, Win32Helper.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            _rightTextBox.Invalidate();
        }

        private static bool IsScrolledToBottom(IntPtr hWnd)
        {
            var si = new Win32Helper.SCROLLINFO
            {
                cbSize = Marshal.SizeOf(typeof(Win32Helper.SCROLLINFO)),
                fMask = Win32Helper.SIF_ALL
            };

            if (Win32Helper.GetScrollInfo(hWnd, Win32Helper.SB_VERT, ref si))
            {
                if (si.nPage == 0 || si.nMax <= si.nPage) return true;
                return si.nPos + si.nPage >= si.nMax - 2;
            }

            return true;
        }

        public new void Show()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(Show));
                return;
            }

            EnsureParent();
            base.Show();
            BringToTop();
        }

        public new void Hide()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(Hide));
                return;
            }

            base.Hide();
        }

        public void ForceClose()
        {
            _forceClose = true;
            _stopwatch.Stop();
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(Close));
                return;
            }

            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_forceClose)
            {
                e.Cancel = true;
                Hide();
                Hidden?.Invoke();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.ExitThread();
        }
    }
}