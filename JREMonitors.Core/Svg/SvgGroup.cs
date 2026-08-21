using System;
using System.Collections.Generic;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Svg
{
    public class SvgGroup : SvgNode
    {
        private readonly ResourceSlot<ID2D1Geometry> _geometrySlot;

        public SvgGroup(RenderContext context) : base(context)
        {
            _geometrySlot = new ResourceSlot<ID2D1Geometry>(() =>
            {
                var geometries = new List<ID2D1Geometry>();
                var count = Children.Count;
                for (var i = 0; i < count; i++)
                {
                    var child = Children[i];
                    if (!child.IsVisible.Value) continue;

                    var g = child.GetBaseGeometry();
                    if (g != null) geometries.Add(g);
                }

                if (geometries.Count > 0)
                    return Factory.CreateGeometryGroup(FillMode.Winding, geometries.ToArray());
                return null;
            });
        }

        public List<SvgNode> Children { get; } = new List<SvgNode>();

        public override event Action OnInvalidated
        {
            add => _geometrySlot.OnInvalidated += value;
            remove => _geometrySlot.OnInvalidated -= value;
        }

        public override ID2D1Geometry GetBaseGeometry()
        {
            return _geometrySlot.Value;
        }

        public override bool Update(bool force)
        {
            var changed = false;
            var count = Children.Count;

            for (var i = 0; i < count; i++)
                if (Children[i].Update(force))
                    changed = true;

            if (_geometrySlot.Update(force || changed)) changed = true;

            return changed;
        }

        public override void ClearCachedGeometry()
        {
            _geometrySlot.Update(true);

            var count = Children.Count;
            for (var i = 0; i < count; i++) Children[i].ClearCachedGeometry();
        }

        public override void Dispose()
        {
            _geometrySlot.Dispose();
            var count = Children.Count;
            for (var i = 0; i < count; i++) Children[i].Dispose();
            base.Dispose();
        }
    }
}