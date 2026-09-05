using System;
using System.Collections.Generic;
using JREMonitors.Core.Providers;

namespace JREMonitors.Core.Services
{
    public enum BlockingLevel
    {
        /// <summary>
        ///     仅阻塞当前屏幕。
        /// </summary>
        CurrentScreen,

        /// <summary>
        ///     阻塞所有屏幕。
        /// </summary>
        AllScreens
    }

    /// <summary>
    ///     阻塞服务，在限定时间窗内按类型与作用域（当前屏幕/全部屏幕）阻塞监视器的交互行为。
    /// </summary>
    public class BlockingService : ITickUpdatable
    {
        private readonly Dictionary<string, Func<IReadOnlyList<string>>> _monitorScreenGetters =
            new Dictionary<string, Func<IReadOnlyList<string>>>();

        private readonly List<BlockingRecord> _queue = new List<BlockingRecord>();

        private string _activeMonitorId;

        public virtual void Update(TimeSpan elapsed)
        {
            var elapsedSeconds = (float)elapsed.TotalSeconds;
            for (var i = _queue.Count - 1; i >= 0; i--)
            {
                var record = _queue[i];
                record.RemainingTime -= elapsedSeconds;
                if (record.RemainingTime <= 0)
                {
                    _queue.RemoveAt(i);
                    record.OnComplete?.Invoke();
                }
            }
        }

        public void RegisterMonitor(string monitorId, Func<IReadOnlyList<string>> activeScreenIdsGetter)
        {
            _monitorScreenGetters[monitorId] = activeScreenIdsGetter;
        }

        public void RemoveMonitor(string monitorId)
        {
            _monitorScreenGetters.Remove(monitorId);
            if (_activeMonitorId == monitorId) _activeMonitorId = null;
        }

        public void SetCurrentMonitor(string monitorId)
        {
            _activeMonitorId = monitorId;
        }

        public void ClearCurrentMonitor()
        {
            _activeMonitorId = null;
        }

        private IReadOnlyList<string> GetActiveScreenIds(string monitorId)
        {
            if (monitorId != null && _monitorScreenGetters.TryGetValue(monitorId, out var getter)) return getter();

            return null;
        }

        public void RequestBlock<T>(
            string blockType,
            BlockingLevel level,
            T data,
            Action<T> callback,
            float duration
        )
        {
            var activeMonitorId = _activeMonitorId;
            var activeScreenIds = GetActiveScreenIds(activeMonitorId);

            var record = new BlockingRecord
            {
                BlockType = blockType,
                Level = level,
                OnComplete = () => callback?.Invoke(data),
                RemainingTime = duration,
                SourceMonitorId = activeMonitorId,
                SourceScreenIds = activeScreenIds
            };
            _queue.Add(record);
        }

        public void RequestBlock(
            string blockType,
            BlockingLevel level,
            Action callback,
            float duration
        )
        {
            var activeMonitorId = _activeMonitorId;
            var activeScreenIds = GetActiveScreenIds(activeMonitorId);

            var record = new BlockingRecord
            {
                BlockType = blockType,
                Level = level,
                OnComplete = callback,
                RemainingTime = duration,
                SourceMonitorId = activeMonitorId,
                SourceScreenIds = activeScreenIds
            };
            _queue.Add(record);
        }

        public bool IsBlocked(string blockType)
        {
            var currentMonitorId = _activeMonitorId;
            var currentScreenIds = GetActiveScreenIds(currentMonitorId);

            if (currentScreenIds == null || currentScreenIds.Count == 0) return false;

            for (var i = 0; i < _queue.Count; i++)
            {
                var record = _queue[i];
                if (record.BlockType != blockType) continue;

                switch (record.Level)
                {
                    case BlockingLevel.CurrentScreen:
                        if (record.SourceMonitorId == currentMonitorId &&
                            Intersects(record.SourceScreenIds, currentScreenIds))
                            return true;
                        break;

                    case BlockingLevel.AllScreens:
                        if (Intersects(record.SourceScreenIds, currentScreenIds))
                            return true;
                        break;
                }
            }

            return false;
        }

        private static bool Intersects(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left == null || right == null) return false;
            var leftCount = left.Count;
            var rightCount = right.Count;
            for (var i = 0; i < leftCount; i++)
            {
                var l = left[i];
                for (var j = 0; j < rightCount; j++)
                    if (l == right[j])
                        return true;
            }

            return false;
        }

        public void Clear()
        {
            _queue.Clear();
        }

        private class BlockingRecord
        {
            public string BlockType { get; set; }
            public BlockingLevel Level { get; set; }
            public Action OnComplete { get; set; }
            public float RemainingTime { get; set; }
            public string SourceMonitorId { get; set; }
            public IReadOnlyList<string> SourceScreenIds { get; set; }
        }
    }
}