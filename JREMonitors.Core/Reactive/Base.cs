using System;

namespace JREMonitors.Core.Reactive
{
    /// <summary>
    ///     响应式节点状态
    /// </summary>
    public enum NodeState
    {
        /// <summary>
        ///     数据最新，无需计算
        /// </summary>
        Clean,

        /// <summary>
        ///     间接依赖可能改变，需要向上游确认
        /// </summary>
        Check,

        /// <summary>
        ///     直接依赖已改变，必须重新计算
        /// </summary>
        Dirty
    }

    /// <summary>
    ///     可失效接口，上游依赖变化时被调用。
    /// </summary>
    public interface IInvalidatable
    {
        void OnDependencyInvalidated(NodeState state);
    }

    public interface ITrackable
    {
        void Track();
    }

    /// <summary>
    ///     依赖追踪器接口：支持记录双向依赖，用于动态解绑 diff。
    /// </summary>
    public interface IDependencyTracker : IInvalidatable
    {
        void RecordDependency(ISignal signal);
    }


    public interface ISignal
    {
        long Version { get; }
        void AddSubscriber(IInvalidatable subscriber);
        void RemoveSubscriber(IInvalidatable subscriber);

        /// <summary>
        ///     拉取并确保自身处于最新状态
        /// </summary>
        void Pull();
    }

    /// <summary>
    ///     可读信号源：具备值读取能力的信号，PropertySlot 通过此接口统一访问 Signal / Computed / PropertySlot。
    /// </summary>
    public interface IValueSignal<out T> : ISignal, ITrackable
    {
        T Value { get; }
    }

    /// <summary>
    ///     批量提交参与者（Signal 侧）：在 Batch flush Phase 1+2 时按顺序操作。
    /// </summary>
    internal interface ICommitable
    {
        /// <summary>Phase 1: 将缓冲值提交到正式字段</summary>
        void Commit();

        /// <summary>Phase 2: 通知下游订阅者</summary>
        void NotifyPending();
    }

    public interface IBatchConsumer
    {
        void ResolvePendingCheck();
    }

    public interface IResourceSlot : IDisposable
    {
        event Action OnInvalidated;
        bool Update(bool force);
    }
}