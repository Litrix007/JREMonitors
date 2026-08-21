using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JREMonitors.SandBox
{
    public static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(int value);

        [STAThread]
        private static void Main()
        {
            SetProcessDpiAwarenessContext(-4);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SandboxForm());
        }
    }
}