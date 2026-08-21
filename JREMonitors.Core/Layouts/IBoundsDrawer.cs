using System;
using System.Drawing;
using JREMonitors.Core.Reactive;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts
{
    public interface IBoundsDrawer : ITrackable, IDisposable
    {
        void Draw(RectangleF targetBounds, Color4 color);
    }
}