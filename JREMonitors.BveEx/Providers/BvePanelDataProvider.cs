using System;
using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BvePanelDataProvider : IPanelDataProvider
    {
        private readonly Lazy<AtsPlugin> _atsPlugin;
        private IReadOnlyDictionary<string, Func<int>> _fallbacks;
        private IReadOnlyDictionary<string, IReadOnlyList<int>> _inputs;

        public BvePanelDataProvider(
            Lazy<AtsPlugin> atsPlugin,
            IReadOnlyDictionary<string, IReadOnlyList<int>> inputs,
            IReadOnlyDictionary<string, Func<int>> fallbacks = null)
        {
            _atsPlugin = atsPlugin ?? throw new ArgumentNullException(nameof(atsPlugin));
            _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            _fallbacks = fallbacks;
        }

        public int GetRawValue(string id)
        {
            if (_inputs.TryGetValue(id, out var indices))
                if (_atsPlugin.Value.TryGetMaxPanelData(indices, out var maxValue))
                    return maxValue;

            if (_fallbacks != null && _fallbacks.TryGetValue(id, out var fallback)) return fallback();

            return 0;
        }

        public bool IsActive(string id)
        {
            return GetRawValue(id) != 0;
        }

        public void Reconfigure(
            IReadOnlyDictionary<string, IReadOnlyList<int>> inputs,
            IReadOnlyDictionary<string, Func<int>> fallbacks = null)
        {
            _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            _fallbacks = fallbacks;
        }

        public bool HasInput(string id)
        {
            return _inputs.ContainsKey(id) || (_fallbacks != null && _fallbacks.ContainsKey(id));
        }
    }
}