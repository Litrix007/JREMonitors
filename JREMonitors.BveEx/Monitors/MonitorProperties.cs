using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;

namespace JREMonitors.BveEx.Monitors
{
    public class MonitorProperties
    {
        public MonitorProperties(
            string id,
            string externalTitle,
            IEnumerable<Size> recommendedSizes,
            Size size,
            MonitorConfig monitorConfig,
            DisplayConfig displayConfig,
            LightingProperties lighting,
            Func<RenderContext, IEnumerable<Screen>> screenFactory,
            string initialScreenId
        ) : this(
            id,
            externalTitle,
            recommendedSizes,
            size,
            monitorConfig.Cab,
            monitorConfig.External,
            lighting,
            displayConfig.GhostingDecayTime,
            displayConfig.ShowUiDebugRect,
            screenFactory,
            initialScreenId
        )
        {
        }

        public MonitorProperties(
            string id,
            string externalTitle,
            IEnumerable<Size> recommendedSizes,
            Size size,
            CabProjectionConfig cab,
            ExternalDisplayConfig external,
            LightingProperties lighting,
            float ghostingDecayTimeSeconds,
            bool showDebugRect,
            Func<RenderContext, IEnumerable<Screen>> screenFactory,
            string initialScreenId
        )
        {
            Id = id;
            ExternalTitle = externalTitle;
            RecommendedSizes = recommendedSizes.ToList();
            Size = size;
            Cab = cab;
            External = external;
            Lighting = lighting;
            GhostingDecayTimeSeconds = ghostingDecayTimeSeconds;
            ShowDebugRect = showDebugRect;
            ScreenFactory = screenFactory;
            InitialScreenId = initialScreenId;
        }

        public string Id { get; }
        public List<Size> RecommendedSizes { get; }
        public Size Size { get; }
        public float GhostingDecayTimeSeconds { get; }
        public bool ShowDebugRect { get; }
        public Func<RenderContext, IEnumerable<Screen>> ScreenFactory { get; }
        public string InitialScreenId { get; }
        public CabProjectionConfig Cab { get; }
        public string ExternalTitle { get; }
        public ExternalDisplayConfig External { get; }
        public LightingProperties Lighting { get; }
    }
}