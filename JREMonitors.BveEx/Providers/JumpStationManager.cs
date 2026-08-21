using System.Collections.Generic;
using JREMonitors.Core.Managers;

namespace JREMonitors.BveEx.Providers
{
    public class JumpStationManager
    {
        private readonly ExecutionGraph<IJumpStationListener> _graph = new ExecutionGraph<IJumpStationListener>();
        private List<IJumpStationListener> _sortedListeners;

        public void InvokeListeners()
        {
            if (_sortedListeners == null) _sortedListeners = _graph.Solve();

            for (var i = 0; i < _sortedListeners.Count; i++) _sortedListeners[i].OnJumpStation();
        }

        public void Register(
            IJumpStationListener listener,
            IEnumerable<IJumpStationListener> before = null,
            IEnumerable<IJumpStationListener> after = null)
        {
            _graph.Add(listener, before, after);
            _sortedListeners = null;
        }

        public void Clear()
        {
            _graph.Clear();
            _sortedListeners = null;
        }
    }
}