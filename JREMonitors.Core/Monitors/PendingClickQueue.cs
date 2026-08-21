using System.Numerics;

namespace JREMonitors.Core.Monitors
{
    public class PendingClickQueue<T>
    {
        private readonly object _lock = new object();
        private bool _hasPending;
        private Vector2 _pos;
        private T _target;

        public bool TryEnqueue(T target, Vector2 pos)
        {
            lock (_lock)
            {
                if (_hasPending) return false;
                _target = target;
                _pos = pos;
                _hasPending = true;
                return true;
            }
        }

        public bool TryDequeue(out T target, out Vector2 pos)
        {
            lock (_lock)
            {
                if (!_hasPending)
                {
                    target = default;
                    pos = default;
                    return false;
                }

                target = _target;
                pos = _pos;
                _hasPending = false;
                _target = default;
                return true;
            }
        }
    }
}