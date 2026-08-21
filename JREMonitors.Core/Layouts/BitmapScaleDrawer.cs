using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts
{
    public class BitmapScaleDrawer : IContentMeasurableBoundsDrawer, IContentHashable
    {
        private readonly ContentArrangement _arrangement;
        private readonly bool _bypassBakerOn1X;
        private readonly RenderContext _context;
        private readonly Baker _dynamicBaker;
        private readonly float _horizontalAlignment;
        private readonly List<DrawerProperties> _propertiesList;
        private readonly Computed<BitmapScaleDrawerSnapshot> _snapshot;
        private readonly bool _snapToPixels;
        private readonly float _spacing;
        private readonly float _verticalAlignment;

        public BitmapScaleDrawer(
            RenderContext context,
            IEnumerable<DrawerProperties> drawerProperties,
            float spacing = 0,
            bool snapToPixels = true,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            ContentArrangement? arrangement = null,
            bool bypassBakerOn1X = true
        )
        {
            _context = context;
            _propertiesList = drawerProperties.ToList();
            _spacing = spacing;
            _snapToPixels = snapToPixels;
            _horizontalAlignment = horizontalAlignment;
            _verticalAlignment = verticalAlignment;
            _arrangement = arrangement ?? ContentArrangement.Center;
            _bypassBakerOn1X = bypassBakerOn1X;
            _dynamicBaker = new Baker(context, BakerPrescaleMode.AutoCubic);
            _snapshot = new Computed<BitmapScaleDrawerSnapshot>(() => new BitmapScaleDrawerSnapshot(this));
        }

        public BitmapScaleDrawer(
            RenderContext context,
            IContentMeasurableBoundsDrawer drawer,
            float scaleX,
            float scaleY,
            bool snapToPixels = true,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float finalOffsetCrossAxis = 0,
            ContentArrangement? arrangement = null,
            bool bypassBakerOn1X = true,
            bool cacheBaker = true
        ) : this(context,
            new[] { new DrawerProperties(drawer, scaleX, scaleY, finalOffsetCrossAxis, null, cacheBaker) }, 0f,
            snapToPixels, horizontalAlignment, verticalAlignment, arrangement, bypassBakerOn1X)
        {
        }

        public IContentSnapshot ToSnapshot()
        {
            return _snapshot.Value;
        }

        public void Draw(RectangleF targetBounds, Color4 color)
        {
            _context.DeviceContext.GetWorldScale(out var worldScaleX, out var worldScaleY);
            TraverseLayout(targetBounds, (originalTargetBounds, bakeBounds, itemDestBounds, prop) =>
            {
                var effectiveColor = prop.Color ?? color;
                var totalScaleX = worldScaleX * prop.ScaleX;
                var totalScaleY = worldScaleY * prop.ScaleY;
                var is1XScale = Math.Abs(totalScaleX - 1) < Epsilons.FloatEpsilon &&
                                Math.Abs(totalScaleY - 1) < Epsilons.FloatEpsilon;

                if (_bypassBakerOn1X && is1XScale)
                {
                    var oldTransform = _context.DeviceContext.Transform;
                    _context.DeviceContext.Transform =
                        Matrix3x2.CreateTranslation(-bakeBounds.X, -bakeBounds.Y)
                        * Matrix3x2.CreateScale(prop.ScaleX, prop.ScaleY)
                        * Matrix3x2.CreateTranslation(itemDestBounds.X, itemDestBounds.Y)
                        * oldTransform;
                    prop.Drawer.Draw(originalTargetBounds, effectiveColor);
                    _context.DeviceContext.Transform = oldTransform;
                    return;
                }

                if (prop.CacheBaker && prop.Drawer is IContentHashable hashable)
                {
                    var contentKey = new BakerContentKey(
                        hashable.ToSnapshot(),
                        originalTargetBounds.Size,
                        bakeBounds.Size,
                        effectiveColor,
                        prop.ScaleX,
                        prop.ScaleY,
                        prop.FinalOffsetCrossAxis
                    );

                    var baker = _context.GetBakerCache().GetOrCreateBaker<BitmapScaleDrawer, BakerContentKey>(
                        _context,
                        contentKey,
                        BakerPrescaleMode.AutoCubic
                    );

                    baker.BakeAndDraw(
                        bakeBounds,
                        () => prop.Drawer.Draw(originalTargetBounds, effectiveColor),
                        itemDestBounds
                    );
                }
                else
                {
                    _dynamicBaker.Refresh();
                    _dynamicBaker.BakeAndDraw(
                        bakeBounds,
                        () => prop.Drawer.Draw(originalTargetBounds, effectiveColor),
                        itemDestBounds
                    );
                }
            });
        }

        public RectangleF GetContentBounds(RectangleF targetBounds)
        {
            return TraverseLayout(targetBounds, null);
        }

        public void Dispose()
        {
            _snapshot?.Dispose();
            var count = _propertiesList.Count;
            for (var i = 0; i < count; i++) _propertiesList[i].Drawer?.Dispose();

            _propertiesList.Clear();
            _dynamicBaker.Dispose();
        }

        public void Track()
        {
            for (var i = 0; i < _propertiesList.Count; i++)
                if (_propertiesList[i].Drawer is ITrackable trackable)
                    trackable.Track();
        }

        private RectangleF TraverseLayout(
            RectangleF targetBounds,
            Action<RectangleF, RectangleF, RectangleF, DrawerProperties> onProcessItem)
        {
            if (_snapToPixels) targetBounds = targetBounds.SnapToPixels();
            var count = _propertiesList.Count;
            var pool = ArrayPool<RectangleF>.Shared;
            var bakeBoundsList = pool.Rent(count);
            try
            {
                var totalScaledWidth = 0f;
                var validCount = 0;
                var maxScaledHeight = 0f;

                for (var i = 0; i < count; i++)
                {
                    var prop = _propertiesList[i];
                    var contentBounds = prop.Drawer.GetContentBounds(targetBounds);
                    if (contentBounds.IsEmpty)
                    {
                        bakeBoundsList[i] = RectangleF.Empty;
                        continue;
                    }

                    var bakeBounds = _snapToPixels ? contentBounds.SnapToPixels() : contentBounds;
                    bakeBoundsList[i] = bakeBounds;
                    var scaledH = bakeBounds.Height * prop.ScaleY;
                    if (scaledH > maxScaledHeight) maxScaledHeight = scaledH;
                    totalScaledWidth += bakeBounds.Width * prop.ScaleX;
                    validCount++;
                }

                if (validCount == 0) return RectangleF.Empty;

                var totalWidthWithSpacing = totalScaledWidth + (validCount - 1) * _spacing;
                var x = targetBounds.X + (targetBounds.Width - totalWidthWithSpacing) * _horizontalAlignment;
                var boxY = targetBounds.Y + (targetBounds.Height - maxScaledHeight) * _verticalAlignment;

                if (_snapToPixels)
                {
                    x = (float)Math.Floor(x);
                    boxY = (float)Math.Floor(boxY);
                }

                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;

                for (var i = 0; i < count; i++)
                {
                    var prop = _propertiesList[i];
                    var bakeBounds = bakeBoundsList[i];
                    if (bakeBounds.IsEmpty) continue;

                    var scaledW = bakeBounds.Width * prop.ScaleX;
                    var scaledH = bakeBounds.Height * prop.ScaleY;

                    float yOffset;
                    switch (_arrangement)
                    {
                        case ContentArrangement.Near:
                            yOffset = 0f;
                            break;
                        case ContentArrangement.Far:
                            yOffset = maxScaledHeight - scaledH;
                            break;
                        case ContentArrangement.Center:
                        case ContentArrangement.Step:
                        default:
                            yOffset = (maxScaledHeight - scaledH) * 0.5f;
                            break;
                    }

                    var y = boxY + yOffset + prop.FinalOffsetCrossAxis;

                    var itemDestBounds = new RectangleF(x, y, scaledW, scaledH);
                    if (_snapToPixels) itemDestBounds = itemDestBounds.SnapToPixels(false);

                    onProcessItem?.Invoke(targetBounds, bakeBounds, itemDestBounds, prop);

                    minX = Math.Min(minX, itemDestBounds.Left);
                    minY = Math.Min(minY, itemDestBounds.Top);
                    maxX = Math.Max(maxX, itemDestBounds.Right);
                    maxY = Math.Max(maxY, itemDestBounds.Bottom);

                    x += scaledW + _spacing;
                    if (_snapToPixels) x = (float)Math.Floor(x);
                }

                if (minX > maxX || minY > maxY) return RectangleF.Empty;
                return new RectangleF(minX, minY, maxX - minX, maxY - minY);
            }
            finally
            {
                pool.Return(bakeBoundsList);
            }
        }

        public class DrawerProperties
        {
            public DrawerProperties(
                IContentMeasurableBoundsDrawer drawer,
                float scaleX,
                float scaleY,
                float finalOffsetCrossAxis = 0,
                Color4? color = null,
                bool cacheBaker = true
            )
            {
                Drawer = drawer;
                ScaleX = scaleX;
                ScaleY = scaleY;
                FinalOffsetCrossAxis = finalOffsetCrossAxis;
                Color = color;
                CacheBaker = cacheBaker;
            }

            public IContentMeasurableBoundsDrawer Drawer { get; }
            public float ScaleX { get; }
            public float ScaleY { get; }
            public float FinalOffsetCrossAxis { get; }
            public Color4? Color { get; }
            public bool CacheBaker { get; }
        }

        private readonly struct BakerContentKey : IEquatable<BakerContentKey>
        {
            private readonly IContentSnapshot _drawer;
            private readonly SizeF _targetSize;
            private readonly SizeF _bakeSize;
            private readonly Color4 _effectiveColor;
            private readonly float _scaleX;
            private readonly float _scaleY;
            private readonly float _finalOffsetCrossAxis;

            public BakerContentKey(
                IContentSnapshot drawer,
                SizeF targetSize,
                SizeF bakeSize,
                Color4 effectiveColor,
                float scaleX,
                float scaleY,
                float finalOffsetCrossAxis)
            {
                _drawer = drawer;
                _targetSize = targetSize;
                _bakeSize = bakeSize;
                _effectiveColor = effectiveColor;
                _scaleX = scaleX;
                _scaleY = scaleY;
                _finalOffsetCrossAxis = finalOffsetCrossAxis;
            }

            public bool Equals(BakerContentKey o)
            {
                if (Math.Abs(_scaleX - o._scaleX) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_scaleY - o._scaleY) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_finalOffsetCrossAxis - o._finalOffsetCrossAxis) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_bakeSize.Width - o._bakeSize.Width) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_bakeSize.Height - o._bakeSize.Height) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_targetSize.Width - o._targetSize.Width) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_targetSize.Height - o._targetSize.Height) > Epsilons.FloatEpsilon) return false;
                if (!_effectiveColor.Equals(o._effectiveColor)) return false;
                if (_drawer == null && o._drawer == null) return true;
                if (_drawer == null || o._drawer == null) return false;
                if (_drawer.ContentHash != o._drawer.ContentHash) return false;
                return _drawer.Equals(o._drawer);
            }

            public override bool Equals(object obj)
            {
                return obj is BakerContentKey o && Equals(o);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    _drawer?.ContentHash ?? 0, _bakeSize, _targetSize, _effectiveColor,
                    _scaleX, _scaleY, _finalOffsetCrossAxis);
            }
        }

        public sealed class BitmapScaleDrawerSnapshot : IContentSnapshot, IEquatable<BitmapScaleDrawerSnapshot>
        {
            private readonly PropEntry[] _entries;

            public BitmapScaleDrawerSnapshot(BitmapScaleDrawer src)
            {
                var props = src._propertiesList;
                _entries = new PropEntry[props.Count];
                var hc = new HashCode();
                for (var i = 0; i < props.Count; i++)
                {
                    var p = props[i];
                    IContentSnapshot childSnap = null;
                    IContentMeasurableBoundsDrawer childRef = null;
                    if (p.Drawer is IContentHashable h)
                    {
                        childSnap = h.ToSnapshot();
                        hc.Add(childSnap.ContentHash);
                    }
                    else
                    {
                        childRef = p.Drawer;
                        hc.Add(childRef?.GetHashCode() ?? 0);
                    }

                    _entries[i] = new PropEntry(childSnap, childRef, p.ScaleX, p.ScaleY,
                        p.FinalOffsetCrossAxis, p.Color);
                    hc.Add(p.ScaleX);
                    hc.Add(p.ScaleY);
                    hc.Add(p.FinalOffsetCrossAxis);
                    hc.Add(p.Color);
                }

                ContentHash = hc.ToHashCode();
            }

            public int ContentHash { get; }

            public bool Equals(IContentSnapshot other)
            {
                return other is BitmapScaleDrawerSnapshot s && Equals(s);
            }

            public bool Equals(BitmapScaleDrawerSnapshot o)
            {
                if (o == null) return false;
                if (_entries.Length != o._entries.Length) return false;
                for (var i = 0; i < _entries.Length; i++)
                {
                    var a = _entries[i];
                    var b = o._entries[i];
                    if (Math.Abs(a.ScaleX - b.ScaleX) > Epsilons.FloatEpsilon) return false;
                    if (Math.Abs(a.ScaleY - b.ScaleY) > Epsilons.FloatEpsilon) return false;
                    if (Math.Abs(a.FinalOffsetCrossAxis - b.FinalOffsetCrossAxis) > Epsilons.FloatEpsilon)
                        return false;
                    if (!Nullable.Equals(a.Color, b.Color)) return false;
                    if (a.ChildSnapshot != null || b.ChildSnapshot != null)
                    {
                        if (a.ChildSnapshot == null || b.ChildSnapshot == null) return false;
                        if (a.ChildSnapshot.ContentHash != b.ChildSnapshot.ContentHash) return false;
                        if (!a.ChildSnapshot.Equals(b.ChildSnapshot)) return false;
                    }
                    else if (!ReferenceEquals(a.ChildRef, b.ChildRef))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is BitmapScaleDrawerSnapshot s && Equals(s);
            }

            public override int GetHashCode()
            {
                return ContentHash;
            }

            private readonly struct PropEntry
            {
                public readonly IContentSnapshot ChildSnapshot;
                public readonly IContentMeasurableBoundsDrawer ChildRef;
                public readonly float ScaleX;
                public readonly float ScaleY;
                public readonly float FinalOffsetCrossAxis;
                public readonly Color4? Color;

                public PropEntry(IContentSnapshot childSnapshot, IContentMeasurableBoundsDrawer childRef,
                    float scaleX, float scaleY, float finalOffsetCrossAxis, Color4? color)
                {
                    ChildSnapshot = childSnapshot;
                    ChildRef = childRef;
                    ScaleX = scaleX;
                    ScaleY = scaleY;
                    FinalOffsetCrossAxis = finalOffsetCrossAxis;
                    Color = color;
                }
            }
        }
    }
}