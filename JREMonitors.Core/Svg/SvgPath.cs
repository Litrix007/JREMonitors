using JREMonitors.Core.Contexts;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Svg
{
    public class SvgPath : SvgNode
    {
        private readonly ID2D1PathGeometry _geometry;

        public SvgPath(RenderContext context, string d) : base(context)
        {
            _geometry = context.D2D1Factory.CreatePathGeometry();
            SvgParser.ParsePathAndFill(d, _geometry);
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