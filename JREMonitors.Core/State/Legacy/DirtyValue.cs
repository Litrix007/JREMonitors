using JREMonitors.Core.Reactive;

namespace JREMonitors.Core.State.Legacy
{
    public class DirtyValue<T> : IVersionedDirty, ITransactional
    {
        private bool _inTransaction;
        private T _tempValue;
        private T _value;

        public DirtyValue(T initial = default)
        {
            _value = initial;
            _tempValue = default;
            _inTransaction = false;
        }

        public T Value
        {
            get => _inTransaction ? _tempValue : _value;
            set
            {
                if (_inTransaction)
                {
                    _tempValue = value;
                }
                else
                {
                    if (!Signal<T>.IsValueChanged(_value, value)) return;
                    _value = value;
                    IsDirty = true;
                    Version++;
                }
            }
        }

        public void BeginChange()
        {
            if (_inTransaction) return;
            _inTransaction = true;
            _tempValue = _value;
        }

        public void EndChange()
        {
            if (!_inTransaction) return;
            _inTransaction = false;

            if (Signal<T>.IsValueChanged(_value, _tempValue))
            {
                _value = _tempValue;
                IsDirty = true;
                Version++;
            }

            _tempValue = default;
        }

        public ulong Version { get; private set; }

        public bool IsDirty { get; private set; }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }


        public static implicit operator T(DirtyValue<T> dirtyValue)
        {
            return dirtyValue != null ? dirtyValue.Value : default;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "null";
        }
    }
}