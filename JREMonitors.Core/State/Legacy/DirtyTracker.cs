using System.Collections.Generic;

namespace JREMonitors.Core.State.Legacy
{
    public class DirtyTracker : ITransactional
    {
        private List<IDirty> _dirties;
        public IReadOnlyList<IDirty> Dirties => _dirties ?? new List<IDirty>();

        public void BeginChange()
        {
            if (_dirties == null) return;
            for (var i = 0; i < _dirties.Count; i++)
            {
                if (!(_dirties[i] is ITransactional transactional)) continue;
                transactional.BeginChange();
            }
        }

        public void EndChange()
        {
            if (_dirties == null) return;
            for (var i = 0; i < _dirties.Count; i++)
            {
                if (!(_dirties[i] is ITransactional transactional)) continue;
                transactional.EndChange();
            }
        }

        public DirtyValue<T> CreateDirtyValue<T>(T initial = default)
        {
            var dv = new DirtyValue<T>(initial);
            RegisterDirty(dv);
            return dv;
        }

        public void RegisterDirty(IDirty dirty)
        {
            if (dirty == null) return;
            if (_dirties == null) _dirties = new List<IDirty>();
            _dirties.Add(dirty);
        }

        public bool HasDirty()
        {
            if (_dirties == null) return false;
            for (var i = 0; i < _dirties.Count; i++)
                if (_dirties[i].IsDirty)
                    return true;
            return false;
        }

        public void MarkChildrenDirty()
        {
            if (_dirties == null) return;
            for (var i = 0; i < _dirties.Count; i++) _dirties[i].MarkDirty();
        }

        public void ClearDirty()
        {
            if (_dirties == null) return;
            for (var i = 0; i < _dirties.Count; i++) _dirties[i].ClearDirty();
        }
    }
}