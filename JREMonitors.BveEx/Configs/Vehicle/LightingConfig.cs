using System.Numerics;
using Vortice.Mathematics;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class LightingConfig
    {
        /// <summary>
        ///     是否启用动态光照，默认为<c>true</c>。
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     亮光完全生效的临界点。
        /// </summary>
        public Vector2? BrightExtent { get; set; }

        /// <summary>
        ///     阴影完全生效的临界点。
        /// </summary>
        public Vector2? ShadowExtent { get; set; }

        /// <summary>
        ///     夜间暗适应提亮倍数。此值在各车型中已配置默认值。
        /// </summary>
        public float? NightAdaptationGain { get; set; }

        /// <summary>
        ///     在夜间调低亮度时亮度下降的快慢程度。此值在各车型中已配置默认值。
        /// </summary>
        public float? NightDimmingResponse { get; set; }

        /// <summary>
        ///     夜间高光压缩门限。此值在各车型中已配置默认值。
        /// </summary>
        public float? CompressThresholdNight { get; set; }

        /// <summary>
        ///     环境光上限。
        /// </summary>
        public float? AmbientMax { get; set; }

        /// <summary>
        ///     LCD物理对比度。此值在各车型中已配置默认值。
        /// </summary>
        public float? PanelContrastRatio { get; set; }

        /// <summary>
        ///     LCD漏光基准色调。此值在各车型中已配置默认值。
        /// </summary>
        public Color3? LeakColor { get; set; }

        /// <summary>
        ///     玻璃亮部反射率。
        /// </summary>
        public float GlassReflectanceLight { get; set; }

        /// <summary>
        ///     玻璃暗部反射率。
        /// </summary>
        public float GlassReflectanceDark { get; set; }

        /// <summary>
        ///     玻璃反光色调。
        /// </summary>
        public Color3 GlareColor { get; set; }
    }
}