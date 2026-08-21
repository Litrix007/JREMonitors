using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace JREMonitors.Core.Reactive
{
    public class Computed<T> : IValueSignal<T>, IDependencyTracker, IDisposable
    {
        private readonly Func<T, T> _converter;
        private readonly bool _enableDynamicUnbinding;

        private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();
        private readonly Func<T> _supplier;
        private T _cachedValue;
        private Dictionary<ISignal, long> _depBuffer;
        private Dictionary<ISignal, long> _dependencyVersions = new Dictionary<ISignal, long>();
        private bool _hasValue;
        private bool _isEvaluating;
        private NodeState _state = NodeState.Dirty;

        public Computed(Func<T> supplier, bool enableDynamicUnbinding = true, Func<T, T> converter = null)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            _enableDynamicUnbinding = enableDynamicUnbinding;
            _converter = converter;
        }

        internal string DebugValue => _cachedValue?.ToString() ?? "null";

        public void OnDependencyInvalidated(NodeState state)
        {
            if (_isEvaluating) return;

            // 状态转换时只广播一次，防止广播风暴
            if (state == NodeState.Dirty && _state != NodeState.Dirty)
            {
                _state = NodeState.Dirty;
                NotifySubscribers(NodeState.Check);
            }
            else if (state == NodeState.Check && _state == NodeState.Clean)
            {
                _state = NodeState.Check;
                NotifySubscribers(NodeState.Check);
            }
        }

        public void RecordDependency(ISignal signal)
        {
            // 在动态收集阶段，记录依赖并捕获其当前版本号
            _dependencyVersions[signal] = signal.Version;
        }

        public void Dispose()
        {
            foreach (var dep in _dependencyVersions.Keys) dep.RemoveSubscriber(this);

            _dependencyVersions.Clear();
            _depBuffer?.Clear();
            _subscribers.Clear();
        }

        public void Track()
        {
            _ = Value;
        }

        public long Version { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                ReactiveScope.RecordSubscription(this);
                Pull();
                return _cachedValue;
            }
        }

        public void Pull()
        {
            UpdateIfNecessary();
        }

        public void AddSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        private void UpdateIfNecessary()
        {
            if (_state == NodeState.Clean && _hasValue) return;

            // 如果处于 Check 状态，级联拉取依赖项的最新状态
            if (_state == NodeState.Check)
            {
                foreach (var dep in _dependencyVersions.Keys) dep.Pull();

                // 检查自上次计算以来，依赖的版本号是否有任何变化
                var anyDependencyChanged = false;
                foreach (var pair in _dependencyVersions)
                    if (pair.Key.Version > pair.Value)
                    {
                        anyDependencyChanged = true;
                        break;
                    }

                if (!anyDependencyChanged)
                {
                    // 依赖的版本未改变，安全截断
                    _state = NodeState.Clean;
                    return;
                }
            }

            Evaluate();
        }

        private void Evaluate()
        {
            if (_isEvaluating) return;
            _isEvaluating = true;
            _state = NodeState.Clean;
            if (_enableDynamicUnbinding)
            {
                if (_depBuffer == null) _depBuffer = new Dictionary<ISignal, long>();
                _depBuffer.Clear();
                (_dependencyVersions, _depBuffer) = (_depBuffer, _dependencyVersions);
            }
            else
            {
                _dependencyVersions.Clear();
            }

            var prevSubscriber = ReactiveScope.CurrentSubscriber;
            ReactiveScope.CurrentSubscriber = this;

            var oldValue = _cachedValue;
            var hasValue = _hasValue;

            try
            {
                var rawValue = _supplier();
                _cachedValue = _converter != null ? _converter(rawValue) : rawValue;
                _hasValue = true;
            }
            finally
            {
                ReactiveScope.CurrentSubscriber = prevSubscriber;
                _isEvaluating = false;
            }

            if (_enableDynamicUnbinding && _depBuffer != null)
                foreach (var oldDep in _depBuffer.Keys)
                    if (!_dependencyVersions.ContainsKey(oldDep))
                        oldDep.RemoveSubscriber(this);

            // 变值检测：只有计算结果真实改变时才自增版本号，下游才能感知到变动
            if (hasValue && Signal<T>.IsValueChanged(oldValue, _cachedValue)) Version++;
        }

        private void NotifySubscribers(NodeState state)
        {
            var count = _subscribers.Count;
            if (count == 0) return;

            var pool = ArrayPool<IInvalidatable>.Shared;
            var snapshot = pool.Rent(count);
            try
            {
                _subscribers.CopyTo(snapshot);
                for (var i = 0; i < count; i++) snapshot[i].OnDependencyInvalidated(state);
            }
            finally
            {
                pool.Return(snapshot, true);
            }
        }

        public static implicit operator T(Computed<T> computed)
        {
            return computed != null ? computed.Value : default;
        }
    }
}