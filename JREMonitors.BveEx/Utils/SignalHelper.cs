using System;
using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.JRE.Constants;

namespace JREMonitors.BveEx.Utils
{
    public static class SignalHelper
    {
        private static readonly IReadOnlyList<(string, float)> AtcSpeedMappings = new[]
        {
            (DirectInputIds.AtcSpeed0, 0f),
            (DirectInputIds.AtcSpeed15, 15),
            (DirectInputIds.AtcSpeed25, 25),
            (DirectInputIds.AtcSpeed45, 45),
            (DirectInputIds.AtcSpeed55, 55),
            (DirectInputIds.AtcSpeed65, 65),
            (DirectInputIds.AtcSpeed75, 75),
            (DirectInputIds.AtcSpeed90, 90),
            (DirectInputIds.AtcSpeed100, 100),
            (DirectInputIds.AtcSpeed110, 110),
            (DirectInputIds.AtcSpeed120, 120)
        };

        public static Func<int> CreateAtc6SpeedLimitFallback(Lazy<AtsPlugin> atsPlugin,
            IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            var activeMappings = new List<AtcSpeedMapping>();
            foreach (var (id, speedLimit) in AtcSpeedMappings)
                if (finalInputs.TryGetValue(id, out var panelIndices))
                    activeMappings.Add(new AtcSpeedMapping(panelIndices, speedLimit));

            return () =>
            {
                for (var i = 0; i < activeMappings.Count; i++)
                {
                    var indices = activeMappings[i].PanelIndices;
                    if (AtsPluginHelper.IsIndicesDisabled(indices)) continue;
                    if (atsPlugin.Value.TryGetMaxPanelData(indices, out var maxValue) && maxValue > 0)
                        return (int)activeMappings[i].SpeedLimit;
                }

                return 0;
            };
        }

        public static Func<int> CreateSingleFallback(Lazy<AtsPlugin> atsPlugin,
            string id, IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            if (!finalInputs.TryGetValue(id, out var indices) || AtsPluginHelper.IsIndicesDisabled(indices))
                return () => 0;

            return () => atsPlugin.Value.TryGetMaxPanelData(indices, out var maxValue) ? maxValue : 0;
        }

        private struct AtcSpeedMapping
        {
            public readonly IReadOnlyList<int> PanelIndices;
            public readonly float SpeedLimit;

            public AtcSpeedMapping(IReadOnlyList<int> panelIndices, float speedLimit)
            {
                PanelIndices = panelIndices;
                SpeedLimit = speedLimit;
            }
        }
    }
}