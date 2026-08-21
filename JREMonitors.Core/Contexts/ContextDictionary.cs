using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JREMonitors.Core.Contexts
{
    public class ContextDictionary : IDictionary<PropertyKey, object>, IDisposable
    {
        private readonly Dictionary<PropertyKey, object> _local = new Dictionary<PropertyKey, object>();
        private readonly IDictionary<PropertyKey, object> _parent;

        public ContextDictionary(IDictionary<PropertyKey, object> parent)
        {
            _parent = parent;
        }

        public object this[PropertyKey key]
        {
            get => TryGetValue(key, out var val) ? val : throw new KeyNotFoundException($"Key '{key}' not found.");
            set => _local[key] = value;
        }

        public ICollection<PropertyKey> Keys =>
            _local.Keys.Union(_parent?.Keys ?? Enumerable.Empty<PropertyKey>()).ToList();

        public ICollection<object> Values => Keys.Select(k => this[k]).ToList();
        public int Count => Keys.Count;
        public bool IsReadOnly => false;

        public void Add(PropertyKey key, object value)
        {
            _local.Add(key, value);
        }

        public bool ContainsKey(PropertyKey key)
        {
            return _local.ContainsKey(key) || (_parent?.ContainsKey(key) ?? false);
        }

        public bool Remove(PropertyKey key)
        {
            return _local.Remove(key);
        }

        public bool TryGetValue(PropertyKey key, out object value)
        {
            if (_local.TryGetValue(key, out value)) return true;
            return _parent != null && _parent.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<PropertyKey, object> item)
        {
            _local.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _local.Clear();
        }

        public bool Contains(KeyValuePair<PropertyKey, object> item)
        {
            return ContainsKey(item.Key) && Equals(this[item.Key], item.Value);
        }

        public void CopyTo(KeyValuePair<PropertyKey, object>[] array, int arrayIndex)
        {
            var list = Keys.Select(k => new KeyValuePair<PropertyKey, object>(k, this[k])).ToList();
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<PropertyKey, object> item)
        {
            return Contains(item) && Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<PropertyKey, object>> GetEnumerator()
        {
            return Keys.Select(k => new KeyValuePair<PropertyKey, object>(k, this[k])).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var value in _local.Values)
                if (value is IDisposable disposable)
                    disposable.Dispose();

            _local.Clear();
        }
    }
}