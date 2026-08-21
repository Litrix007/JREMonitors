using System;
using System.Collections.Generic;
using JREMonitors.Core.Providers;

namespace JREMonitors.Core.Managers
{
    public class TickUpdateManager : ITickUpdatable
    {
        private readonly ExecutionGraph<ITickUpdatable> _graph = new ExecutionGraph<ITickUpdatable>();
        private List<ITickUpdatable> _sortedUpdatables;

        public void Update(TimeSpan elapsed)
        {
            if (_sortedUpdatables == null) _sortedUpdatables = _graph.Solve();

            for (var i = 0; i < _sortedUpdatables.Count; i++) _sortedUpdatables[i].Update(elapsed);
        }

        public void Register(
            ITickUpdatable updatable,
            IEnumerable<ITickUpdatable> before = null,
            IEnumerable<ITickUpdatable> after = null)
        {
            _graph.Add(updatable, before, after);
            _sortedUpdatables = null;
        }

        public void Clear()
        {
            _graph.Clear();
            _sortedUpdatables = null;
        }
    }
}