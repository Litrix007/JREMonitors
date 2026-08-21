using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JREMonitors.BveEx.Monitors
{
    public class BveWindowHook : NativeWindow
    {
        private const int SWP_NOSIZE = 0x0001;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_WINDOWPOSCHANGED = 0x0047;
        private const int WM_SIZE = 0x0005;

        private bool _inSizeMove;

        public BveWindowHook(IntPtr hWnd)
        {
            AssignHandle(hWnd);
        }

        public event EventHandler BeforeResize;
        public event EventHandler AfterResize;

        protected override void WndProc(ref Message m)
        {
            var isSizeChangingMessage = false;
            var isSingleActionMessage = false;

            switch (m.Msg)
            {
                case WM_ENTERSIZEMOVE:
                    _inSizeMove = true;
                    isSizeChangingMessage = true;
                    break;

                case WM_EXITSIZEMOVE:
                    _inSizeMove = false;
                    isSizeChangingMessage = true;
                    isSingleActionMessage = true;
                    break;

                case WM_WINDOWPOSCHANGING:
                    var posChanging = (WindowPos)Marshal.PtrToStructure(m.LParam, typeof(WindowPos));
                    if ((posChanging.flags & SWP_NOSIZE) == 0) isSizeChangingMessage = true;
                    break;

                case WM_WINDOWPOSCHANGED:
                    var posChanged = (WindowPos)Marshal.PtrToStructure(m.LParam, typeof(WindowPos));
                    if ((posChanged.flags & SWP_NOSIZE) == 0) isSizeChangingMessage = true;
                    break;

                case WM_SIZE:
                    isSizeChangingMessage = true;
                    if (!_inSizeMove) isSingleActionMessage = true;
                    break;
            }

            if (isSizeChangingMessage) BeforeResize?.Invoke(this, EventArgs.Empty);

            base.WndProc(ref m);

            if (isSizeChangingMessage)
                if (isSingleActionMessage)
                    AfterResize?.Invoke(this, EventArgs.Empty);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowPos
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }
    }
}