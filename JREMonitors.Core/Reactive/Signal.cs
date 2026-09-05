using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JREMonitors.Core.Constants;

namespace JREMonitors.Core.Reactive
{
    /// <summary>
    ///     可变响应式信号，持有单个 <typeparamref name="T" /> 值，值变更时驱动下游订阅者失效。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         用法：ViewModel 暴露信号（如 <c>public Signal&lt;bool&gt; SupportsTasc { get; } = new();</c>），
    ///         在 <c>OnUpdate</c> 中赋值 <c>SupportsTasc.Value = ...</c>；Widget 侧将其
    ///         <see cref="PropertySlot{T}.Bind" /> 到属性槽，或直接作为 <c>WatchEffect</c> 的跟踪项，
    ///         读值即建立依赖，值变化触发 Invalidate，效果重跑。
    ///     </para>
    ///     <para>
    ///         赋值处于批量作用域内时先缓冲到临时值，Batch 提交（Commit→NotifyPending）时统一生效；
    ///         同值写入由 <see cref="IsValueChanged" /> 短路、不产生通知。
    ///     </para>
    /// </remarks>
    public class Signal<T> : IValueSignal<T>, ICommitable
    {
        private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();
        private bool _committed;
        private bool _hasPendingWrite;
        private T _tempValue;
        private T _value;

        public Signal(T initial = default) : this(initial, 0)
        {
        }

        internal Signal(T initial, long initialVersion)
        {
            _value = initial;
            Version = initialVersion;
        }

        internal T DebugValue => _hasPendingWrite ? _tempValue : _value;

        public void Commit()
        {
            if (!_hasPendingWrite) return;
            _hasPendingWrite = false;

            if (IsValueChanged(_value, _tempValue))
            {
                _value = _tempValue;
                _tempValue = default;
                Version++;
                _committed = true;
            }
            else
            {
                _tempValue = default;
                _committed = false;
            }
        }

        public void NotifyPending()
        {
            if (!_committed) return;
            _committed = false;
            NotifySubscribers(NodeState.Dirty);
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
                return _hasPendingWrite ? _tempValue : _value;
            }
            set
            {
                if (!ReactiveScope.IsBatching && !ReactiveScope.IsFlushing)
                {
                    if (!IsValueChanged(_value, value)) return;
                    _value = value;
                    Version++;
                    NotifySubscribers(NodeState.Dirty);
                    return;
                }

                _tempValue = value;
                if (!_hasPendingWrite)
                {
                    _hasPendingWrite = true;
                    ReactiveScope.RegisterPendingSignal(this);
                }
            }
        }

        public void Pull()
        {
            // Signal 始终是最新状态，无需任何操作
        }

        public void AddSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        private void NotifySubscribers(NodeState state)
        {
            foreach (var sub in _subscribers) sub.OnDependencyInvalidated(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValueChanged(T oldValue, T newValue)
        {
            if (typeof(T) == typeof(float))
                return Math.Abs((float)(object)oldValue - (float)(object)newValue) > Epsilons.FloatEpsilon;

            if (typeof(T) == typeof(double))
                return Math.Abs((double)(object)oldValue - (double)(object)newValue) > Epsilons.DoubleEpsilon;

            if (typeof(T) == typeof(bool)) return (bool)(object)oldValue != (bool)(object)newValue;

            if (typeof(T) == typeof(int)) return (int)(object)oldValue != (int)(object)newValue;

            if (typeof(T) == typeof(string))
                return !string.Equals((string)(object)oldValue, (string)(object)newValue, StringComparison.Ordinal);

            if (!typeof(T).IsValueType && ReferenceEquals(oldValue, newValue)) return false;

            return !EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }

        public static implicit operator T(Signal<T> signal)
        {
            return signal.Value;
        }
    }
}