using System;
using System.Collections.Generic;

namespace JREMonitors.Core.State
{
    /// <summary>
    ///     领域/服务层容器，按类型注册与解析非渲染层的服务、数据提供者与共享状态对象，支持父链解析。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         架构层级位于渲染层之外，渲染期资源一律放入 <see cref="JREMonitors.Core.Contexts.RenderContext.Properties" />，
    ///         本容器只登记逻辑层可组合的服务与状态。
    ///     </para>
    /// </remarks>
    public class DataHub : IDisposable
    {
        private readonly DataHub _parent;
        private readonly Dictionary<Type, ValueEntry> _storage = new Dictionary<Type, ValueEntry>();

        public DataHub(DataHub parent = null)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            Clear();
        }

        public void Put<T>(T provider, bool autoDispose = false) where T : class
        {
            _storage[typeof(T)] = new ValueEntry(provider, autoDispose);
        }

        public void Remove<T>()
        {
            _storage.Remove(typeof(T));
        }

        public T GetOrNull<T>() where T : class
        {
            var targetType = typeof(T);

            if (_storage.TryGetValue(targetType, out var entry)) return (T)entry.Value;

            foreach (var pair in _storage)
                if (targetType.IsAssignableFrom(pair.Key))
                {
                    _storage[targetType] = pair.Value;
                    return (T)pair.Value.Value;
                }

            return _parent?.GetOrNull<T>();
        }

        public T Get<T>() where T : class
        {
            var provider = GetOrNull<T>();
            return provider ??
                   throw new InvalidOperationException($"Failed to resolve {typeof(T)} from {nameof(DataHub)}.");
        }

        public void Clear()
        {
            foreach (var entry in _storage.Values)
                if (entry.AutoDispose && entry.Value is IDisposable disposable)
                    disposable.Dispose();

            _storage.Clear();
        }

        private struct ValueEntry
        {
            public readonly object Value;
            public readonly bool AutoDispose;

            public ValueEntry(object value, bool autoDispose)
            {
                Value = value;
                AutoDispose = autoDispose;
            }
        }
    }
}