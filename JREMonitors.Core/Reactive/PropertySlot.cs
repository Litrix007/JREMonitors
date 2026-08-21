using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace JREMonitors.Core.Reactive
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class PropertySlot<T> : IValueSignal<T>, IInvalidatable, IDisposable, IBatchConsumer
    {
        private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();
        private bool _isPendingCheck;
        private Signal<T> _ownedSignal;
        private long _prevVersion;
        private IValueSignal<T> _source;

        public PropertySlot(T initialValue = default)
        {
            AttachOwnedSignal(initialValue);
            _prevVersion = Version;
        }

        public PropertySlot(IValueSignal<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.AddSubscriber(this);
            _prevVersion = Version;
        }

        private bool IsOwned => ReferenceEquals(_source, _ownedSignal);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (IsOwned)
                    return $"{nameof(PropertySlot<T>)}[Signal] {_ownedSignal?.DebugValue?.ToString() ?? "null"}";
                if (_source is Computed<T> c)
                    return $"{nameof(PropertySlot<T>)}[Computed] {c.DebugValue ?? "null"}";
                if (_source is PropertySlot<T> p)
                    return $"{nameof(PropertySlot<T>)}[BoundSlot] {p.Value?.ToString() ?? "null"}";
                return
                    $"{nameof(PropertySlot<T>)}[BoundSignal] {(_source as Signal<T>)?.DebugValue?.ToString() ?? "null"}";
            }
        }

        public void ResolvePendingCheck()
        {
            if (!_isPendingCheck) return;
            _isPendingCheck = false;
            if (OnInvalidated == null) return;
            Pull();
            var currentVersion = Version;
            if (currentVersion <= _prevVersion) return;
            _prevVersion = currentVersion;
            OnInvalidated.Invoke();
        }

        public void Dispose()
        {
            DetachSource();
            AttachOwnedSignal(default);
            _subscribers.Clear();
            OnInvalidated = null;
        }

        public void OnDependencyInvalidated(NodeState state)
        {
            if (state == NodeState.Dirty)
            {
                _isPendingCheck = false;
                OnInvalidated?.Invoke();
                _prevVersion = Version;
                NotifySubscribers(state);
            }
            else if (state == NodeState.Check)
            {
                if (OnInvalidated != null)
                {
                    if (ReactiveScope.IsBatching || ReactiveScope.IsFlushing)
                    {
                        _isPendingCheck = true;
                        ReactiveScope.RegisterPendingConsumer(this);
                    }
                    else
                    {
                        _isPendingCheck = true;
                        ResolvePendingCheck();
                    }
                }
                else
                {
                    _isPendingCheck = true;
                }

                NotifySubscribers(state);
            }
        }

        public void Track()
        {
            _ = Value;
        }

        public long Version => _source.Version;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                ReactiveScope.RecordSubscription(this);
                var prev = ReactiveScope.CurrentSubscriber;
                ReactiveScope.CurrentSubscriber = null;
                try
                {
                    return _source.Value;
                }
                finally
                {
                    ReactiveScope.CurrentSubscriber = prev;
                }
            }
            set
            {
                if (!IsOwned)
                {
                    var prev = ReactiveScope.CurrentSubscriber;
                    ReactiveScope.CurrentSubscriber = null;
                    T fallback;
                    try
                    {
                        fallback = _source.Value;
                    }
                    finally
                    {
                        ReactiveScope.CurrentSubscriber = prev;
                    }

                    var oldVersion = Version;
                    DetachSource();
                    AttachOwnedSignal(fallback, oldVersion);
                }

                _ownedSignal.Value = value;
            }
        }

        public void Pull()
        {
            _source.Pull();
        }

        public void AddSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(IInvalidatable subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        public event Action OnInvalidated;

        public void Bind(IValueSignal<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            DetachSource();
            _source = source;
            _ownedSignal = null;
            _source.AddSubscriber(this);
            OnInvalidated?.Invoke();
            _prevVersion = Version;
            NotifySubscribers(NodeState.Dirty);
        }

        public void Unbind()
        {
            if (!IsOwned)
            {
                var oldVersion = Version;
                var prev = ReactiveScope.CurrentSubscriber;
                ReactiveScope.CurrentSubscriber = null;
                T fallbackValue;
                try
                {
                    fallbackValue = _source.Value;
                }
                finally
                {
                    ReactiveScope.CurrentSubscriber = prev;
                }

                DetachSource();
                AttachOwnedSignal(fallbackValue, oldVersion);
            }
        }

        private void AttachOwnedSignal(T initialValue, long initialVersion = 1)
        {
            _ownedSignal = new Signal<T>(initialValue, initialVersion);
            _source = _ownedSignal;
            _source.AddSubscriber(this);
        }

        private void DetachSource()
        {
            if (_source != null)
            {
                _source.RemoveSubscriber(this);
                _source = null;
                _ownedSignal = null;
            }
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

        public static implicit operator T(PropertySlot<T> slot)
        {
            return slot != null ? slot.Value : default;
        }
    }
}