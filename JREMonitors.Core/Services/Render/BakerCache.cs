using System;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;

namespace JREMonitors.Core.Services.Render
{
    public class BakerCache : TypedResourceCache<Baker>
    {
        public static readonly PropertyKey Key = new PropertyKey(nameof(BakerCache));

        public Baker GetOrCreateBaker<TComponent, TKey>(
            RenderContext context,
            TKey contentKey,
            BakerPrescaleMode mode = BakerPrescaleMode.Vector
        )
            where TComponent : class
            where TKey : struct, IEquatable<TKey>
        {
            var key = new BakerCacheKey<TKey>(typeof(TComponent), contentKey, mode);
            return GetOrCreate(key, (context.DeviceContext, mode), s => new Baker(s.DeviceContext, s.mode));
        }
    }

    public readonly struct BakerCacheKey<TKey> : IEquatable<BakerCacheKey<TKey>>
        where TKey : struct, IEquatable<TKey>
    {
        private readonly Type _componentType;
        private readonly TKey _contentKey;
        private readonly BakerPrescaleMode _mode;

        public BakerCacheKey(Type componentType, TKey contentKey, BakerPrescaleMode mode)
        {
            _componentType = componentType;
            _contentKey = contentKey;
            _mode = mode;
        }

        public bool Equals(BakerCacheKey<TKey> other)
        {
            return _componentType == other._componentType &&
                   _mode == other._mode &&
                   _contentKey.Equals(other._contentKey);
        }

        public override bool Equals(object obj)
        {
            return obj is BakerCacheKey<TKey> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_componentType, _contentKey.GetHashCode(), (int)_mode);
        }
    }

    public static class BakerRenderServiceExtensions
    {
        public static BakerCache GetBakerCache(this RenderContext context)
        {
            return context.GetService<BakerCache>(BakerCache.Key);
        }
    }
}