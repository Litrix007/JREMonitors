using JREMonitors.Core.Monitors;
using Vortice.Mathematics;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class DisplayConfig
    {
        private int _bufferFrameCount;
        private float _ghostingDecayTime = MonitorOutput.DefaultGhostingDecayTimeSeconds;

        /// <summary>
        ///     监视器画面残影衰减时间（单位：秒；默认<c>0.1</c>，最小<c>0</c>，最大<c>0.5</c>）。
        /// </summary>
        /// <remarks>
        ///     <para>设为0可提高性能。</para>
        /// </remarks>
        public float GhostingDecayTime
        {
            get => _ghostingDecayTime;
            set => _ghostingDecayTime = MathHelper.Clamp(value, 0, 0.5f);
        }

        /// <summary>
        ///     帧缓冲区大小。设为0时使用默认值。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         D3D9 模式：默认2，最小1，最大3；画面恒定延迟<c>N-1</c>帧。
        ///         <br />设为1以上的值可显著降低耗时。
        ///     </para>
        ///     <para>
        ///         D3D9Ex 模式：默认3，最小3，最大5；
        ///         <br />常规稳态延迟1帧，视GPU负载在<c>[0, N-2]</c>帧之间自适应。
        ///         <br />若积压超过<c>N-2</c>帧将保持上一画面并触发背压停产。
        ///     </para>
        /// </remarks>
        public int BufferFrameCount
        {
            get => _bufferFrameCount;
            set => _bufferFrameCount = MathHelper.Clamp(value, 0, 5);
        }

        /// <summary>
        ///     调试选项：显示调试日志窗口。
        /// </summary>
        public bool ShowDebugWindow { get; set; }

        /// <summary>
        ///     调试选项：显示监视器画面中各组件的绘制边界。
        /// </summary>
        public bool ShowUiDebugRect { get; set; }

        /// <summary>
        ///     调试选项：显示游戏驾驶室内纹理的实际边界。
        /// </summary>
        public bool ShowCabTextureBoundsRect { get; set; }
    }
}