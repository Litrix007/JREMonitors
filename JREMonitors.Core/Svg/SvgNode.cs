using System;
using System.Collections.Generic;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Svg
{
    public abstract class SvgNode : IResourceSlot
    {
        protected readonly ID2D1Factory Factory;

        protected SvgNode(RenderContext context)
        {
            Factory = context.D2D1Factory;
            FillBrush = context.DeviceContext.CreateSolidColorBrush(default);
            StrokeBrush = context.DeviceContext.CreateSolidColorBrush(default);

            IsVisible = new Signal<bool>(true);
            Transform = new Signal<Matrix3x2>(Matrix3x2.Identity);
            Fill = new Signal<Color4?>();
            Stroke = new Signal<Color4?>();
            StrokeWidth = new Signal<float>(1f);
        }

        public string Id { get; set; }
        public Signal<bool> IsVisible { get; }
        public Signal<Matrix3x2> Transform { get; }
        public Signal<Color4?> Fill { get; }
        public Signal<Color4?> Stroke { get; }
        public Signal<float> StrokeWidth { get; }

        public ID2D1SolidColorBrush FillBrush { get; }
        public ID2D1SolidColorBrush StrokeBrush { get; }
        public ID2D1StrokeStyle StrokeStyle { get; set; }
        public Dictionary<string, SvgNode> ElementsMap { get; set; }

        public virtual event Action OnInvalidated
        {
            add { }
            remove { }
        }

        public virtual void Dispose()
        {
            FillBrush?.Dispose();
            StrokeBrush?.Dispose();
            StrokeStyle?.Dispose();
        }

        public abstract bool Update(bool force);

        public abstract ID2D1Geometry GetBaseGeometry();

        public virtual void ClearCachedGeometry()
        {
        }
    }
}