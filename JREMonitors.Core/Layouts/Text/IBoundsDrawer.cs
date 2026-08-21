using System;
using System.Drawing;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts.Text
{
    public interface IBoundsDrawer : IDisposable
    {
        void DrawText(RectangleF bounds, Color4 color);
    }
}