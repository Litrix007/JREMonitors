using System;
using System.Drawing;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Monitors
{
    public class ScreenBackgroundBuffer : IDisposable
    {
        public ScreenBackgroundBuffer(Size size, ID2D1DeviceContext dc)
        {
            BackgroundBitmap = dc.CreateBitmap(size, IntPtr.Zero, 0, RenderHelper.BitMapProperties8Bit);
            HasBackgroundRendered = false;
        }

        public ID2D1Bitmap1 BackgroundBitmap { get; }
        public bool HasBackgroundRendered { get; set; }
        public bool ShowDebugRect { get; set; }

        public void Dispose()
        {
            BackgroundBitmap.Dispose();
        }
    }
}