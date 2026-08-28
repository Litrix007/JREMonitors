using System.Numerics;
using Vortice.Mathematics;

namespace JREMonitors.BveEx.Monitors
{
    public class LightingProperties
    {
        public static readonly LightingProperties Disabled = new LightingProperties();

        private LightingProperties()
        {
            Enabled = false;
        }

        public LightingProperties(
            bool enabled,
            Vector2? brightExtent,
            Vector2? shadowExtent,
            float nightAdaptationGain,
            float nightDimmingResponse,
            float compressThresholdNight,
            float panelContrastRatio,
            Color3 leakColor,
            float glassReflectanceLight,
            float glassReflectanceDark,
            Color3 glareColor,
            float ambientMax
        )
        {
            Enabled = enabled;
            BrightExtent = brightExtent;
            ShadowExtent = shadowExtent;
            NightAdaptationGain = nightAdaptationGain;
            NightDimmingResponse = nightDimmingResponse;
            CompressThresholdNight = compressThresholdNight;
            PanelContrastRatio = panelContrastRatio;
            LeakColor = leakColor;
            GlassReflectanceLight = glassReflectanceLight;
            GlassReflectanceDark = glassReflectanceDark;
            GlareColor = glareColor;
            AmbientMax = ambientMax;
        }

        public bool Enabled { get; }
        public Vector2? BrightExtent { get; }
        public Vector2? ShadowExtent { get; }
        public float NightAdaptationGain { get; }
        public float NightDimmingResponse { get; }
        public float CompressThresholdNight { get; }
        public float PanelContrastRatio { get; }
        public Color3 LeakColor { get; }
        public float GlassReflectanceLight { get; }
        public float GlassReflectanceDark { get; }
        public Color3 GlareColor { get; }
        public float AmbientMax { get; } = 1f;
    }
}