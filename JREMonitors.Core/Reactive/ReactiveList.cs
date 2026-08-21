using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace JREMonitors.Core.Reactive
{
    public class ReactiveList<T> : IReadOnlyList<T>, ISignal, IInvalidatable, ITrackable, ICommitable, IDisposable,
        IBatchConsumer
    {
        private readonly List<T> _items = new List<T>();
        private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();
        private bool _committed;
        private bool _hasPendingWrite;
        private bool _isBoundMode;
        private bool _isPendingCheck;
        private long _lastSeenSourceVersion;
        private long _prevVersion;
        private IValueSignal<IReadOnlyList<T>> _source;
        private NodeState _state = NodeState.Clean;

        public ReactiveList(IEnumerable<T> initial = null, int capacity = 0)
        {
            _items.Capacity = capacity;
            if (initial != null) _items.AddRange(initial);

            _prevVersion = Version;
        }

        public ReactiveList(IValueSignal<IReadOnlyList<T>> source)
        {
            _prevVersion = Version;
            Bind(source);
        }

        public void ResolvePendingCheck()
        {
            if (!_isPendingCheck) return;
            _isPendingCheck = false;
            if (OnInvalidated == null) return;
            Pull();
            if (Version <= _prevVersion) return;
            _prevVersion = Version;
            OnInvalidated.Invoke();
        }

        public void Commit()
        {
            if (!_hasPendingWrite) return;
            _hasPendingWrite = false;
            _committed = true;
            Version++;
        }

        public void NotifyPending()
        {
            if (!_committed) return;
            _committed = false;
            NotifySubscribers(NodeState.Dirty);
        }

        public void Dispose()
        {
            DetachSource();
            _subscribers.Clear();
            OnInvalidated = null;
        }

        public void OnDependencyInvalidated(NodeState state)
        {
            if (!_isBoundMode) return;
            if (state == NodeState.Dirty && _state != NodeState.Dirty)
            {
                _state = NodeState.Dirty;
                _isPendingCheck = false;
                OnInvalidated?.Invoke();
                _prevVersion = Version;
                NotifySubscribers(NodeState.Check);
            }
            else if (state == NodeState.Check && _state == NodeState.Clean)
            {
                _state = NodeState.Check;
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

                NotifySubscribers(NodeState.Check);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T this[int index]
        {
            get
            {
                ReactiveScope.RecordSubscription(this);
                // 按需拉取，确保读取时数据已同步
                Pull();
                return _items[index];
            }
            set
            {
                if (_isBoundMode) Unbind();
                if (!Signal<T>.IsValueChanged(_items[index], value)) return;
                _items[index] = value;
                NotifyChange();
            }
        }

        public int Count
        {
            get
            {
                ReactiveScope.RecordSubscription(this);
                // 按需拉取
                Pull();
                return _items.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            ReactiveScope.RecordSubscription(this);
            // 按需拉取
            Pull();
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public long Version { get; private set; }

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

        public void Track()
        {
            ReactiveScope.RecordSubscription(this);
        }

        public event Action OnInvalidated;

        public void Bind(IValueSignal<IReadOnlyList<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            DetachSource();
            _source = source;
            _isBoundMode = true;
            _source.AddSubscriber(this);
            // 绑定为全新节点，设为 Dirty 强制在首次读取时执行同步，并向外广播 Dirty
            _state = NodeState.Dirty;
            OnInvalidated?.Invoke();
            _prevVersion = Version;
            NotifySubscribers(NodeState.Dirty);
        }

        public void Unbind()
        {
            if (_isBoundMode && _source != null)
            {
                Pull();
                DetachSource();
            }
        }

        private void DetachSource()
        {
            if (_isBoundMode && _source != null)
            {
                _source.RemoveSubscriber(this);
                _source = null;
                _isBoundMode = false;
                _state = NodeState.Clean;
            }
        }

        private void UpdateIfNecessary()
        {
            if (!_isBoundMode || _source == null) return;
            if (_state == NodeState.Clean) return;
            _state = NodeState.Clean;
            _source.Pull();
            if (_source.Version <= _lastSeenSourceVersion) return;
            _lastSeenSourceVersion = _source.Version;
            var prev = ReactiveScope.CurrentSubscriber;
            ReactiveScope.CurrentSubscriber = null;
            try
            {
                var newList = _source.Value;
                var changed = UpdateInternal(newList);
                if (changed) Version++;
            }
            finally
            {
                ReactiveScope.CurrentSubscriber = prev;
            }
        }

        public void Add(T item)
        {
            if (_isBoundMode) Unbind();
            _items.Add(item);
            NotifyChange();
        }

        public bool Remove(T item)
        {
            if (_isBoundMode) Unbind();
            var removed = _items.Remove(item);
            if (removed) NotifyChange();
            return removed;
        }

        public void Clear()
        {
            if (_isBoundMode) Unbind();
            if (_items.Count == 0) return;
            _items.Clear();
            NotifyChange();
        }

        public void Update(IReadOnlyList<T> newItems)
        {
            if (_isBoundMode) Unbind();
            var changed = UpdateInternal(newItems);
            if (changed) NotifyChange();
        }

        private bool UpdateInternal(IReadOnlyList<T> newItems)
        {
            if (newItems == null)
            {
                if (_items.Count > 0)
                {
                    _items.Clear();
                    return true;
                }

                return false;
            }

            var changed = false;
            var newCount = newItems.Count;
            var oldCount = _items.Count;
            var minCount = Math.Min(newCount, oldCount);
            for (var i = 0; i < minCount; i++)
                if (Signal<T>.IsValueChanged(_items[i], newItems[i]))
                {
                    _items[i] = newItems[i];
                    changed = true;
                }

            if (newCount > oldCount)
            {
                for (var i = oldCount; i < newCount; i++) _items.Add(newItems[i]);

                changed = true;
            }
            else if (newCount < oldCount)
            {
                _items.RemoveRange(newCount, oldCount - newCount);
                changed = true;
            }

            return changed;
        }

        private void NotifyChange()
        {
            if (ReactiveScope.IsBatching || ReactiveScope.IsFlushing)
            {
                if (!_hasPendingWrite)
                {
                    _hasPendingWrite = true;
                    ReactiveScope.RegisterPendingSignal(this);
                }

                return;
            }

            Version++;
            NotifySubscribers(NodeState.Dirty);
        }

        private void NotifySubscribers(NodeState state)
        {
            if (state == NodeState.Dirty)
            {
                OnInvalidated?.Invoke();
                _prevVersion = Version;
            }

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
    }
}