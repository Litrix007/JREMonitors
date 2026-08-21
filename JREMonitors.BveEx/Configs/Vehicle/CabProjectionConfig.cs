using System.Numerics;
using System.Text.Json;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Monitors;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class CabProjectionConfig
    {
        /// <summary>
        ///     是否将监视器画面投影到游戏驾驶室内，默认为<c>true</c>。
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     定义监视器纹理的四边形顶点，据此决定3D透视效果，坐标系与Panel文件一致。
        /// </summary>
        /// <remarks>
        ///     必须为凸四边形，且面积必须大于<c>0</c>。
        /// </remarks>
        public Quad2D Positions { get; set; }

        /// <summary>
        ///     纹理的旋转中心点，等价于<see cref="Needle.Origin" />。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         默认为四边形的左上角。
        ///     </para>
        /// </remarks>
        public Vector2? Origin { get; set; } = null;

        /// <summary>
        ///     纹理的旋转角度，单位为度，默认为<c>0</c>。
        /// </summary>
        /// <remarks>
        ///     <para>此属性<b>不影响3D透视效果应用</b>，但合理设置此属性可改善倾斜画面下的显示质量。</para>
        /// </remarks>
        public float Rotation { get; set; } = 0;

        /// <summary>
        ///     纹理绘制顺序，默认显示在最上层，等价于<see cref="Needle.Layer" />。
        /// </summary>
        public int? Layer { get; set; } = null;

        /// <summary>
        ///     纹理绕X轴的旋转角度，单位为度，默认为<c>0</c>。
        /// </summary>
        /// <remarks>
        ///     <para>此属性<b>不影响3D透视效果应用</b>，但合理设置此属性可改善倾斜画面下的显示质量。</para>
        /// </remarks>
        public float TiltX { get; set; } = 0;

        /// <summary>
        ///     纹理绕Y轴的旋转角度，单位为度，默认为<c>0</c>。
        /// </summary>
        /// <remarks>
        ///     <para>此属性<b>不影响3D透视效果应用</b>，但合理设置此属性可改善倾斜画面下的显示质量。</para>
        /// </remarks>
        public float TiltY { get; set; } = 0;

        /// <summary>
        ///     监视器画面圆角半径，默认为<c>0</c>。此值相对于<b>监视器的原始分辨率</b>。
        /// </summary>
        public float BorderRadius { get; set; } = 0;

        /// <summary>
        ///     动态光照配置。
        /// </summary>
        public LightingConfig Lighting { get; set; }

        public void Validate()
        {
            if (Positions.IsAabbEmpty()) throw new JsonException("Cab projection positions must have positive area.");
            if (!Positions.IsConvex()) throw new JsonException("Cab projection positions must be convex quad.");
            if (BorderRadius < 0) throw new JsonException("Cab border radius must be greater than or equal to 0.");
        }
    }
}