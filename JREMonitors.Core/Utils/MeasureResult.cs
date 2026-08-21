using System;
using System.Drawing;
using System.Numerics;
using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils
{
    public struct MeasureResult : IDisposable
    {
        public IDWriteTextLayout2 Layout { get; }
        public float Width { get; }
        public float Height { get; }
        public SizeF Size => new SizeF(Width, Height);

        public MeasureResult(IDWriteTextLayout2 layout, float width, float height)
        {
            Layout = layout;
            Width = width;
            Height = height;
        }

        public Vector2 GetCorrectOffset(bool useHorizontalOverhangMetrics, bool useVerticalOverhangMetrics,
            float horizontalAlignment = 0, float verticalAlignment = 0)
        {
            var offsetX = -horizontalAlignment * Width;
            if (useHorizontalOverhangMetrics) offsetX += Layout.OverhangMetrics.Left;
            var offsetY = -verticalAlignment * Height;
            if (useVerticalOverhangMetrics) offsetY += Layout.OverhangMetrics.Top;
            return new Vector2(offsetX, offsetY);
        }

        public void Dispose()
        {
            Layout.Dispose();
        }
    }
}