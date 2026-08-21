using System;

namespace JREMonitors.Core.Reactive
{
    public class ResourceSlot<T> : IResourceSlot where T : class, IDisposable
    {
        private readonly ReactiveEffect _effect;

        public ResourceSlot(Func<T> factory, bool enableDynamicUnbinding = true)
        {
            _effect = new ReactiveEffect(() =>
            {
                var oldValue = Value;
                Value = null;
                oldValue?.Dispose();
                Value = factory();
            }, enableDynamicUnbinding);
        }

        public T Value { get; private set; }

        public event Action OnInvalidated
        {
            add => _effect.OnInvalidated += value;
            remove => _effect.OnInvalidated -= value;
        }

        public bool Update(bool force = false)
        {
            return _effect.Run(force);
        }

        public void Dispose()
        {
            var oldValue = Value;
            Value = null;
            oldValue?.Dispose();
            _effect.Dispose();
        }
    }
}