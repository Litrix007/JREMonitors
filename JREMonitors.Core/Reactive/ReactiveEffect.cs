using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Reactive
{
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