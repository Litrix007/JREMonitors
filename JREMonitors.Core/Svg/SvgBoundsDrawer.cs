using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using Vortice.Mathematics;

namespace JREMonitors.Core.Svg
{
    public class SvgBoundsDrawer : IContentMeasurableBoundsDrawer, IResourceSlot
    {
        private readonly RenderContext _context;

        public SvgBoundsDrawer(RenderContext context, SvgDocumentProperties properties)
        {
            _context = context;
            Document = new SvgDocument(context, properties);
        }

        public SvgDocument Document { get; }

        public void Draw(RectangleF targetBounds, Color4 color)
        {
            var targetWidth = Document.TargetWidth.Value;
            var targetHeight = Document.TargetHeight.Value;

            var scaleX = MathHelper.Min(targetBounds.Width / targetWidth, 1);
            var scaleY = MathHelper.Min(targetBounds.Height / targetHeight, 1);
            var scale = MathHelper.Min(scaleX, scaleY);

            var oldTransform = _context.DeviceContext.Transform;
            var center = new Vector2(targetBounds.X + targetBounds.Width / 2,
                targetBounds.Y + targetBounds.Height / 2);

            var localTransform = Matrix3x2.CreateTranslation(-targetWidth / 2, -targetHeight / 2)
                                 * Matrix3x2.CreateScale(scale)
                                 * Matrix3x2.CreateTranslation(center);

            _context.DeviceContext.Transform = localTransform * oldTransform;

            Document.GlobalColor = color == Colors.Transparent ? (Color4?)null : color;
            Document.Draw(_context.DeviceContext);

            _context.DeviceContext.Transform = oldTransform;
        }

        public RectangleF GetContentBounds(RectangleF targetBounds)
        {
            return new RectangleF(targetBounds.X, targetBounds.Y, Document.TargetWidth.Value,
                Document.TargetHeight.Value);
        }

        public void Dispose()
        {
            Document.Dispose();
        }

        public void Track()
        {
            Document.Track();
        }

        public event Action OnInvalidated
        {
            add => Document.OnInvalidated += value;
            remove => Document.OnInvalidated -= value;
        }

        public bool Update(bool force)
        {
            return Document.Update(force);
        }
    }
}