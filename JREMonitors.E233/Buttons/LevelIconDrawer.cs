using System;
using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using Vortice.Mathematics;

namespace JREMonitors.E233.Buttons
{
    public abstract class LevelIconDrawer : IContentMeasurableBoundsDrawer, IContentHashable
    {
        private readonly Computed<LevelIconDrawerSnapshot> _snapshot;
        protected readonly RenderContext Context;

        protected LevelIconDrawer(RenderContext context, SizeF iconSize)
        {
            Context = context;
            IconSize = iconSize;
            _snapshot = new Computed<LevelIconDrawerSnapshot>(() => new LevelIconDrawerSnapshot(this));
        }

        public SizeF IconSize { get; }
        public PropertySlot<int> Level { get; } = new PropertySlot<int>();

        public IContentSnapshot ToSnapshot()
        {
            return _snapshot.Value;
        }

        public virtual RectangleF GetContentBounds(RectangleF targetBounds)
        {
            return new RectangleF(0, 0, IconSize.Width, IconSize.Height);
        }

        public virtual void Track()
        {
            Level.Track();
        }

        public virtual void Draw(RectangleF targetBounds, Color4 color)
        {
            var level = Level.Value;
            var bakerKey = (GetType(), level, color);
            var baker = Context.GetBakerCache()
                .GetOrCreateBaker<LevelIconDrawer, (Type drawerType, int level, Color4 color)>(
                    Context,
                    bakerKey,
                    BakerPrescaleMode.AutoCubic
                );
            var originX = (float)Math.Round(targetBounds.X + (targetBounds.Width - IconSize.Width) / 2f,
                MidpointRounding.AwayFromZero);
            var originY = (float)Math.Round(targetBounds.Y + (targetBounds.Height - IconSize.Height) / 2f,
                MidpointRounding.AwayFromZero);
            var drawBounds = new RectangleF(originX, originY, IconSize.Width, IconSize.Height);
            baker.BakeAndDraw(drawBounds, () => DrawIcon(originX, originY, color, level));
        }

        public virtual void Dispose()
        {
            _snapshot?.Dispose();
            Level.Dispose();
        }

        protected abstract void DrawIcon(float originX, float originY, Color4 color, int level);

        public sealed class LevelIconDrawerSnapshot : IContentSnapshot, IEquatable<LevelIconDrawerSnapshot>
        {
            private readonly Type _drawerType;
            private readonly int _level;

            public LevelIconDrawerSnapshot(LevelIconDrawer src)
            {
                _drawerType = src.GetType();
                _level = src.Level.Value;
                ContentHash = HashCode.Combine(_drawerType, _level);
            }

            public int ContentHash { get; }

            public bool Equals(IContentSnapshot other)
            {
                return other is LevelIconDrawerSnapshot s && Equals(s);
            }

            public bool Equals(LevelIconDrawerSnapshot o)
            {
                if (o == null) return false;
                return _drawerType == o._drawerType && _level == o._level;
            }

            public override bool Equals(object obj)
            {
                return obj is LevelIconDrawerSnapshot s && Equals(s);
            }

            public override int GetHashCode()
            {
                return ContentHash;
            }
        }
    }
}