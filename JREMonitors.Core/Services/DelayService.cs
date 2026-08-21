using System;
using System.Collections.Generic;
using JREMonitors.Core.Providers;

namespace JREMonitors.Core.Services
{
    public class DelayService : ITickUpdatable
    {
        private readonly Dictionary<string, IDelayProvider> _providers =
            new Dictionary<string, IDelayProvider>();

        public void Update(TimeSpan elapsed)
        {
            foreach (var provider in _providers.Values) provider.Update(elapsed);
        }

        public void Register(string id, IDelayProvider provider)
        {
            _providers[id] = provider;
        }

        public IDelayProvider GetDelayProvider(string id)
        {
            return _providers.TryGetValue(id, out var provider)
                ? provider
                : throw new InvalidOperationException($"Failed to resolve {id} from {nameof(DelayService)}.");
        }
    }
}