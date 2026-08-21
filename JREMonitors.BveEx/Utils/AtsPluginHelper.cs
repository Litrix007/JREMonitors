using System.Collections.Generic;
using System.Linq;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    public static class AtsPluginHelper
    {
        public static bool HasPanelData(this AtsPlugin atsPlugin, int index)
        {
            return index >= 0 && index < atsPlugin.PanelArray.Length;
        }

        public static int GetPanelData(this AtsPlugin atsPlugin, int index)
        {
            if (atsPlugin.HasPanelData(index)) return atsPlugin.PanelArray[index];

            return 0;
        }

        public static bool IsIndicesDisabled(IReadOnlyList<int> indices)
        {
            return indices == null || indices.Count == 0 || indices.Any(index => index < -2);
        }

        public static bool TryGetMaxPanelData(this AtsPlugin atsPlugin, IReadOnlyList<int> indices, out int maxValue)
        {
            if (indices == null || indices.Count == 0)
            {
                maxValue = 0;
                return false;
            }

            var hasValidData = false;
            maxValue = int.MinValue;
            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                if (index == -2)
                {
                    hasValidData = true;
                    if (1 > maxValue) maxValue = 1;
                    continue;
                }

                if (index == -1)
                {
                    hasValidData = true;
                    maxValue = 0;
                    break;
                }

                if (!atsPlugin.HasPanelData(index)) continue;
                var val = atsPlugin.GetPanelData(index);
                if (val > maxValue) maxValue = val;
                hasValidData = true;
            }

            if (!hasValidData) maxValue = 0;

            return hasValidData;
        }
    }
}