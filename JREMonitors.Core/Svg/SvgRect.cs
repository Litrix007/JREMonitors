using JREMonitors.Core.Contexts;
using Vortice;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Svg
{
    public class SvgRect : SvgNode
    {
        private readonly ID2D1Geometry _geometry;

        public SvgRect(RenderContext context, float x, float y, float width, float height, float rx = 0f,
            float ry = 0f) : base(context)
        {
            var rect = new RawRectF(x, y, x + width, y + height);
            if (rx > 0f || ry > 0f)
            {
                var finalRx = rx > 0f ? rx : ry;
                var finalRy = ry > 0f ? ry : rx;
                _geometry = context.D2D1Factory.CreateRoundedRectangleGeometry(
                    new RoundedRectangle(rect, finalRx, finalRy));
            }
            else
            {
                _geometry = context.D2D1Factory.CreateRectangleGeometry(rect);
            }
        }

        public override ID2D1Geometry GetBaseGeometry()
        {
            return _geometry;
        }

        public override bool Update(bool force)
        {
            return false;
        }

        public override void Dispose()
        {
            _geometry?.Dispose();
            base.Dispose();
        }
    }
}