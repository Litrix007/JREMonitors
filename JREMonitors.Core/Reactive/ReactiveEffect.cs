using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Reactive
{
    /// <summary>
    ///     依赖追踪副作用，运行动作时收集依赖，依赖变化时触发 <see cref="OnInvalidated" /> 并可按需重跑。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Run" /> 执行动作并重建依赖集（开启动态解绑时移除不再读取的依赖订阅）；依赖按
    ///         Dirty/Check 分级：直接失效即置 Dirty 并可重跑，Check 态经批处理队列延迟确认，确认有变化才
    ///         触发 <see cref="OnInvalidated" />。
    ///     </para>
    ///     <para>
    ///         用法：Widget 经 <c>WatchEffect(phase, action, enableDynamicUnbinding)</c> 创建——Commit 效果体
    ///         自动注入 Invalidate 以驱动重绘，State/Visual/Commit 三阶段效果分别在对应更新环节由 Widget 调用
    ///         <see cref="Run" />；动作体内读信号即完成依赖声明。
    ///     </para>
    /// </remarks>
    public class ReactiveEffect : IDependencyTracker, IDisposable, IBatchConsumer
    {
        private readonly Action _effectAction;
        private readonly bool _enableDynamicUnbinding;
        private Dictionary<ISignal, long> _depBuffer;
        private Dictionary<ISignal, long> _dependencyVersions = new Dictionary<ISignal, long>();
        private bool _isPendingCheck;
        private bool _isRunning;
        private bool _needsExecution = true;
        private NodeState _state = NodeState.Dirty;

        public ReactiveEffect(Action effectAction, bool enableDynamicUnbinding = true)
        {
            _effectAction = effectAction ?? throw new ArgumentNullException(nameof(effectAction));
            _enableDynamicUnbinding = enableDynamicUnbinding;
        }

        public void ResolvePendingCheck()
        {
            if (!_isPendingCheck) return;
            _isPendingCheck = false;

            // 级联拉取依赖项的版本
            foreach (var dep in _dependencyVersions.Keys) dep.Pull();

            var anyDependencyChanged = false;
            foreach (var pair in _dependencyVersions)
                if (pair.Key.Version > pair.Value)
                {
                    anyDependencyChanged = true;
                    break;
                }

            if (!anyDependencyChanged)
            {
                _state = NodeState.Clean;
                _needsExecution = false;
            }
            else
            {
                _state = NodeState.Dirty;
                _needsExecution = true;
                OnInvalidated?.Invoke();
            }
        }

        public void RecordDependency(ISignal signal)
        {
            _dependencyVersions[signal] = signal.Version;
        }

        public void OnDependencyInvalidated(NodeState state)
        {
            if (_isRunning) return;

            if (state == NodeState.Dirty && _state != NodeState.Dirty)
            {
                _state = NodeState.Dirty;
                _isPendingCheck = false;
                _needsExecution = true;
                OnInvalidated?.Invoke();
            }
            else if (state == NodeState.Check && _state == NodeState.Clean)
            {
                _state = NodeState.Check;
                _isPendingCheck = true;
                if (ReactiveScope.IsBatching || ReactiveScope.IsFlushing)
                    ReactiveScope.RegisterPendingConsumer(this);
                else
                    ResolvePendingCheck();
            }
        }

        public void Dispose()
        {
            foreach (var dep in _dependencyVersions.Keys) dep.RemoveSubscriber(this);

            _dependencyVersions.Clear();
            _depBuffer?.Clear();
            OnInvalidated = null;
        }

        public event Action OnInvalidated;

        public bool Run(bool force = false)
        {
            if (_isRunning) return false;

            if (force)
            {
                _state = NodeState.Dirty;
                _needsExecution = true;
            }

            // 如果当前仍在 Check，拉取并校验
            if (_state == NodeState.Check) ResolvePendingCheck();

            if (!_needsExecution) return false;

            _isRunning = true;
            _state = NodeState.Clean;
            _needsExecution = false;

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

            try
            {
                _effectAction();
            }
            finally
            {
                ReactiveScope.CurrentSubscriber = prevSubscriber;
                _isRunning = false;
            }

            if (_enableDynamicUnbinding && _depBuffer != null)
                foreach (var oldDep in _depBuffer.Keys)
                    if (!_dependencyVersions.ContainsKey(oldDep))
                        oldDep.RemoveSubscriber(this);

            return true;
        }
    }
}