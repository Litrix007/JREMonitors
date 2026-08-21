using System;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Svg
{
    public class SvgUse : SvgNode
    {
        private readonly ResourceSlot<ID2D1Geometry> _geometrySlot;

        public SvgUse(RenderContext context, string targetId) : base(context)
        {
            TargetId = targetId;

            _geometrySlot = new ResourceSlot<ID2D1Geometry>(() =>
            {
                if (ElementsMap == null || !ElementsMap.TryGetValue(TargetId, out var target)) return null;
                if (!target.IsVisible.Value) return null;

                var baseGeom = target.GetBaseGeometry();
                if (baseGeom != null) return Factory.CreateTransformedGeometry(baseGeom, Transform.Value);

                return null;
            });
        }

        public string TargetId { get; }

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
            return _geometrySlot.Update(force);
        }

        public override void ClearCachedGeometry()
        {
            _geometrySlot.Update(true);
        }

        public override void Dispose()
        {
            _geometrySlot.Dispose();
            base.Dispose();
        }
    }
}