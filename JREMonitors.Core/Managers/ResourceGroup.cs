using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Managers
{
    public class ResourceGroup : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Dispose()
        {
            for (var i = 0; i < _disposables.Count; i++)
                try
                {
                    _disposables[i]?.Dispose();
                }
                catch (Exception)
                {
                }

            _disposables.Clear();
        }

        public T Track<T>(T resource) where T : IDisposable
        {
            if (resource != null && !_disposables.Contains(resource)) _disposables.Add(resource);

            return resource;
        }
    }
}