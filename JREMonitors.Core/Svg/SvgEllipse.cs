using System.Numerics;
using JREMonitors.Core.Contexts;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Svg
{
    public class SvgEllipse : SvgNode
    {
        private readonly ID2D1EllipseGeometry _geometry;

        public SvgEllipse(RenderContext context, float cx, float cy, float rx, float ry) : base(context)
        {
            _geometry = context.D2D1Factory.CreateEllipseGeometry(new Ellipse(new Vector2(cx, cy), rx, ry));
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