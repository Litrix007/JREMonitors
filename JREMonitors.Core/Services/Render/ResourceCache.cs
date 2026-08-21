using System;
using System.Collections.Generic;
using JREMonitors.Core.Contexts;

namespace JREMonitors.Core.Services.Render
{
    public class ResourceCache<TKey, TValue> : IDisposable
        where TValue : class, IDisposable
    {
        private readonly Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();
        private long _version;

        public long Version
        {
            get => _version;
            set
            {
                if (_version >= value) return;
                Clear();
                _version = value;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public TValue GetOrCreate(TKey key, Func<TValue> factory)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                value = factory();
                _cache[key] = value;
            }

            return value;
        }

        public TValue GetOrCreate<TState>(TKey key, TState state, Func<TState, TValue> factory)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                value = factory(state);
                _cache[key] = value;
            }

            return value;
        }

        public void Clear()
        {
            foreach (var val in _cache.Values) val?.Dispose();
            _cache.Clear();
        }
    }


    public class TypedResourceCache<TValue> : IDisposable
        where TValue : class, IDisposable
    {
        private readonly Dictionary<Type, ICacheBucket> _buckets = new Dictionary<Type, ICacheBucket>();
        private long _version;

        public long Version
        {
            get => _version;
            set
            {
                if (_version >= value) return;
                Clear();
                _version = value;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public TValue GetOrCreate<TKey>(TKey key, Func<TValue> factory)
        {
            var keyType = typeof(TKey);
            if (!_buckets.TryGetValue(keyType, out var bucket))
            {
                bucket = new Bucket<TKey>();
                _buckets[keyType] = bucket;
            }

            var typedBucket = (Bucket<TKey>)bucket;
            if (!typedBucket.Cache.TryGetValue(key, out var value))
            {
                value = factory();
                typedBucket.Cache[key] = value;
            }

            return value;
        }

        public TValue GetOrCreate<TKey, TState>(TKey key, TState state, Func<TState, TValue> factory)
        {
            var keyType = typeof(TKey);
            if (!_buckets.TryGetValue(keyType, out var bucket))
            {
                bucket = new Bucket<TKey>();
                _buckets[keyType] = bucket;
            }

            var typedBucket = (Bucket<TKey>)bucket;
            if (!typedBucket.Cache.TryGetValue(key, out var value))
            {
                value = factory(state);
                typedBucket.Cache[key] = value;
            }

            return value;
        }

        private void Clear()
        {
            foreach (var bucket in _buckets.Values) bucket.Dispose();

            _buckets.Clear();
        }

        private interface ICacheBucket : IDisposable
        {
        }

        private class Bucket<TKey> : ICacheBucket
        {
            public readonly Dictionary<TKey, TValue> Cache = new Dictionary<TKey, TValue>();

            public void Dispose()
            {
                foreach (var val in Cache.Values) val?.Dispose();
                Cache.Clear();
            }
        }
    }

    public static class ResourceCacheExtensions
    {
        public static ResourceCache<TKey, TValue> GetResourceCache<TKey, TValue>(this RenderContext context,
            PropertyKey key)
            where TValue : class, IDisposable
        {
            return context.GetService<ResourceCache<TKey, TValue>>(key);
        }
    }
}