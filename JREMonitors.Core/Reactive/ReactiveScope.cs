using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Reactive
{
    public static class ReactiveScope
    {
        [ThreadStatic] internal static IDependencyTracker CurrentSubscriber;

        [ThreadStatic] private static int _batchDepth;
        [ThreadStatic] private static List<ICommitable> _pendingSignals;
        [ThreadStatic] private static HashSet<ICommitable> _pendingSignalSet;

        // 边界消费者暂存队列
        [ThreadStatic] private static List<IBatchConsumer> _pendingConsumers;
        [ThreadStatic] private static HashSet<IBatchConsumer> _pendingConsumerSet;

        public static bool IsBatching => _batchDepth > 0;
        [field: ThreadStatic] public static bool IsFlushing { get; private set; }

        public static void RecordSubscription(ISignal signal)
        {
            var sub = CurrentSubscriber;
            if (sub != null)
            {
                signal.AddSubscriber(sub);
                sub.RecordDependency(signal);
            }
        }

        public static void BeginBatch()
        {
            _batchDepth++;
        }

        public static void EndBatch()
        {
            _batchDepth--;
            if (_batchDepth > 0) return;
            if (_pendingSignals == null) return;

            IsFlushing = true;

            // Phase 1: 提交缓冲数据
            for (var i = 0; i < _pendingSignals.Count; i++) _pendingSignals[i].Commit();

            // Phase 2: 发送变动通知（直接下游转为 Dirty，间接下游转为 Check）
            for (var i = 0; i < _pendingSignals.Count; i++) _pendingSignals[i].NotifyPending();

            IsFlushing = false;
            _pendingSignals.Clear();
            _pendingSignalSet.Clear();
            // Phase 3: 刷新并检查所有潜在变更的边界消费者。
            // 此时才会级联触发 lazy computed 的求值，并自动触发 Invalidate 向上冒泡
            FlushConsumers();
        }

        internal static void RegisterPendingSignal(ICommitable signal)
        {
            EnsureInitialized();
            if (_pendingSignalSet.Add(signal)) _pendingSignals.Add(signal);
        }

        internal static void RegisterPendingConsumer(IBatchConsumer consumer)
        {
            EnsureInitialized();
            if (_pendingConsumerSet.Add(consumer)) _pendingConsumers.Add(consumer);
        }

        private static void EnsureInitialized()
        {
            if (_pendingSignals != null) return;
            _pendingSignals = new List<ICommitable>();
            _pendingSignalSet = new HashSet<ICommitable>();
            _pendingConsumers = new List<IBatchConsumer>();
            _pendingConsumerSet = new HashSet<IBatchConsumer>();
        }

        private static void FlushConsumers()
        {
            if (_pendingConsumers == null) return;

            while (_pendingConsumers.Count > 0)
            {
                var consumers = _pendingConsumers.ToArray();
                _pendingConsumers.Clear();
                _pendingConsumerSet.Clear();

                for (var i = 0; i < consumers.Length; i++) consumers[i].ResolvePendingCheck();
            }
        }

        public static void TrackAll(params ITrackable[] trackables)
        {
            if (trackables == null) return;
            foreach (var trackable in trackables) trackable?.Track();
        }
    }
}