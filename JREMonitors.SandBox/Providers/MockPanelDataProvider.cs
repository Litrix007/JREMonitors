using System.Collections.Generic;
using JREMonitors.Core.Providers;

namespace JREMonitors.SandBox.Providers
{
    public class MockPanelDataProvider : IPanelDataProvider
    {
        private readonly Dictionary<string, int> _data;

        public MockPanelDataProvider(Dictionary<string, int> data)
        {
            _data = data;
        }

        public int GetRawValue(string id)
        {
            if (_data.TryGetValue(id, out var value)) return value;

            return 1;
        }

        public bool IsActive(string id)
        {
            return GetRawValue(id) != 0;
        }
    }
}