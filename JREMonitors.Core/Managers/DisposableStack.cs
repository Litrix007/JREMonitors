using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Managers
{
    public class DisposableStack : IDisposable
    {
        private readonly List<IDisposable> _resources = new List<IDisposable>();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var count = _resources.Count;
            for (var i = count - 1; i >= 0; i--) _resources[i]?.Dispose();

            _resources.Clear();
        }

        public void AddResource(IDisposable resource)
        {
            if (resource == null) return;
            if (_disposed)
            {
                resource.Dispose();
                return;
            }

            _resources.Add(resource);
        }

        public bool RemoveResource(IDisposable resource)
        {
            if (resource == null || _disposed) return false;
            return _resources.Remove(resource);
        }
    }
}