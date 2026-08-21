using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JREMonitors.Core.Reactive
{
    public class ReactiveArray<T> : IReadOnlyList<T>, ISignal, IInvalidatable, ITrackable, ICommitable, IDisposable,
        IBatchConsumer
    {
        private readonly List<int> _committedIndices = new List<int>();
        private readonly T[] _items;
        private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();
        private T[] _enumerationSnapshot;
        private bool _hasAnyPendingWrite;
        private IndexSignal[] _indexSignals;
        private bool _isBoundMode;
        private bool _isPendingCheck;
        private long _lastSeenSourceVersion;
        private bool[] _pendingWrites;
        private long _prevVersion;
        private IValueSignal<IReadOnlyList<T>> _source;
        private NodeState _state = NodeState.Clean;

        // 批处理缓存
        private T[] _tempItems;

        public ReactiveArray(int length)
        {
            _items = new T[length];
            _prevVersion = Version;
        }

        public ReactiveArray(IEnumerable<T> initial)
        {
            _items = initial?.ToArray() ?? Array.Empty<T>();
            _prevVersion = Version;
        }

        public ReactiveArray(int length, IValueSignal<IReadOnlyList<T>> source)
        {
            _items = new T[length];
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
            if (!_hasAnyPendingWrite) return;
            _hasAnyPendingWrite = false;
            _committedIndices.Clear();

            for (var i = 0; i < _items.Length; i++)
                if (_pendingWrites[i])
                {
                    _pendingWrites[i] = false;
                    if (Signal<T>.IsValueChanged(_items[i], _tempItems[i]))
                    {
                        _items[i] = _tempItems[i];
                        _committedIndices.Add(i);
                    }

                    _tempItems[i] = default;
                }

            if (_committedIndices.Count > 0) Version++;
        }

        public void NotifyPending()
        {
            if (_committedIndices.Count == 0) return;

            _prevVersion = Version;
            OnInvalidated?.Invoke();
            NotifySubscribers(NodeState.Dirty);

            foreach (var idx in _committedIndices)
                if (_indexSignals != null && _indexSignals[idx] != null)
                {
                    var idxSig = _indexSignals[idx];
                    idxSig.Version++;
                    idxSig.NotifySubscribers(NodeState.Dirty);
                }

            _committedIndices.Clear();
        }

        public void Dispose()
        {
            DetachSource();
            _subscribers.Clear();
            if (_indexSignals != null)
                for (var i = 0; i < _indexSignals.Length; i++)
                    _indexSignals[i]?.Dispose();

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
                // 收到上游 Dirty 变动时，向下游传递 Check 等待重算
                NotifySubscribers(NodeState.Check);
                NotifyAllIndexSignals(NodeState.Check);
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
                    // 若无事件绑定（OnInvalidated == null），直接标记状态，由下游 Pull 时解决
                    _isPendingCheck = true;
                }

                NotifySubscribers(NodeState.Check);
                NotifyAllIndexSignals(NodeState.Check);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T this[int index]
        {
            get
            {
                // 当通过索引读取时，获取该槽位的独立 Signal 实体用于依赖注册
                var idxSignal = GetOrCreateIndexSignal(index);
                ReactiveScope.RecordSubscription(idxSignal);

                Pull();
                return _hasAnyPendingWrite && _pendingWrites[index] ? _tempItems[index] : _items[index];
            }
            set
            {
                if (_isBoundMode) Unbind();

                if (ReactiveScope.IsBatching || ReactiveScope.IsFlushing)
                {
                    EnsureBatchBuffers();
                    _tempItems[index] = value;

                    if (!_pendingWrites[index])
                    {
                        _pendingWrites[index] = true;
                        if (!_hasAnyPendingWrite)
                        {
                            _hasAnyPendingWrite = true;
                            // 将 Array 本身作为一个 Commit 单元扔进批处理中心
                            ReactiveScope.RegisterPendingSignal(this);
                        }
                    }
                }
                else
                {
                    if (!Signal<T>.IsValueChanged(_items[index], value)) return;
                    _items[index] = value;
                    Version++;
                    NotifyChange(index);
                }
            }
        }

        public int Count
        {
            get
            {
                // 读取 Count 相当于关心整个数组的大小变化
                Track();
                Pull();
                return _items.Length;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Track();
            Pull();
            if (_hasAnyPendingWrite)
            {
                for (var i = 0; i < _items.Length; i++)
                    _enumerationSnapshot[i] = _pendingWrites[i] ? _tempItems[i] : _items[i];
                return ((IEnumerable<T>)_enumerationSnapshot).GetEnumerator();
            }

            return ((IEnumerable<T>)_items).GetEnumerator();
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

        // 只要注册了 Array 本身，任何索引变动都会导致订阅失效
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

            _state = NodeState.Dirty;
            OnInvalidated?.Invoke();
            _prevVersion = Version;
            // 绑定为全新节点，级联触发全局和细粒度代理的失效
            NotifySubscribers(NodeState.Dirty);
            NotifyAllIndexSignals(NodeState.Dirty);
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
                var newItems = _source.Value;
                UpdateInternal(newItems);
            }
            finally
            {
                ReactiveScope.CurrentSubscriber = prev;
            }
        }

        public void Update(IReadOnlyList<T> newItems)
        {
            if (_isBoundMode) Unbind();
            UpdateInternal(newItems);
        }

        private void UpdateInternal(IReadOnlyList<T> newItems)
        {
            if (newItems == null) return;

            var anyChanged = false;
            var changedIndices = new List<int>();
            var minCount = Math.Min(_items.Length, newItems.Count);

            for (var i = 0; i < minCount; i++)
                if (Signal<T>.IsValueChanged(_items[i], newItems[i]))
                {
                    _items[i] = newItems[i];
                    changedIndices.Add(i);
                    anyChanged = true;
                }

            if (anyChanged)
            {
                Version++;
                _prevVersion = Version;
                OnInvalidated?.Invoke();
                // 1. 通知监控全局数组的订阅者
                NotifySubscribers(NodeState.Dirty);
                // 2. 通知变动槽位的独立订阅者
                foreach (var idx in changedIndices)
                    if (_indexSignals?[idx] != null)
                    {
                        var indexSignal = _indexSignals[idx];
                        indexSignal.Version++;
                        indexSignal.NotifySubscribers(NodeState.Dirty);
                    }
            }
        }

        private void EnsureBatchBuffers()
        {
            if (_tempItems == null)
            {
                _tempItems = new T[_items.Length];
                _pendingWrites = new bool[_items.Length];
                _enumerationSnapshot = new T[_items.Length];
            }
        }

        private void NotifyChange(int index)
        {
            // 数组级别的全局版本递增与通知
            _prevVersion = Version;
            OnInvalidated?.Invoke();
            NotifySubscribers(NodeState.Dirty);

            // 槽级别的精准通知
            if (_indexSignals != null && _indexSignals[index] != null)
            {
                var idxSig = _indexSignals[index];
                idxSig.Version++;
                idxSig.NotifySubscribers(NodeState.Dirty);
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

        private void NotifyAllIndexSignals(NodeState state)
        {
            if (_indexSignals == null) return;
            for (var i = 0; i < _indexSignals.Length; i++) _indexSignals[i]?.NotifySubscribers(state);
        }

        private IndexSignal GetOrCreateIndexSignal(int index)
        {
            if (_indexSignals == null) _indexSignals = new IndexSignal[_items.Length];
            if (_indexSignals[index] == null) _indexSignals[index] = new IndexSignal(this);
            return _indexSignals[index];
        }

        private class IndexSignal : ISignal
        {
            private readonly ReactiveArray<T> _parent;
            private readonly HashSet<IInvalidatable> _subscribers = new HashSet<IInvalidatable>();

            public IndexSignal(ReactiveArray<T> parent)
            {
                _parent = parent;
                Version = 1;
            }

            public long Version { get; internal set; }

            public void AddSubscriber(IInvalidatable sub)
            {
                _subscribers.Add(sub);
            }

            public void RemoveSubscriber(IInvalidatable sub)
            {
                _subscribers.Remove(sub);
            }

            public void Pull()
            {
                _parent.Pull();
            }

            internal void NotifySubscribers(NodeState state)
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

            public void Dispose()
            {
                _subscribers.Clear();
            }
        }
    }
}