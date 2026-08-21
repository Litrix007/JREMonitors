using JREMonitors.Core.Monitors;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class ExternalDisplayConfig
    {
        /// <summary>
        ///     监视器加载时是否同步显示外置窗口，默认为<c>false</c>。
        /// </summary>
        /// <remarks>
        ///     <para>可在右键菜单中手动显示或隐藏。</para>
        /// </remarks>
        public bool Show { get; set; } = false;

        /// <summary>
        ///     外置窗口的画面缩放与对齐模式，默认为<c>Letterbox</c>。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         • <c>Letterbox</c>（默认）：保持原始宽高比等比缩放，不足部分留黑边。
        ///         <br />• <c>Center</c>：保持 1:1 原始分辨率居中显示，不缩放。
        ///         <br />• <c>Stretch</c>：拉伸填满整个窗口，不保持宽高比。
        ///     </para>
        /// </remarks>
        public ScreenDisplayMode DisplayMode { get; set; } = ScreenDisplayMode.Letterbox;

        public void Validate()
        {
        }
    }
}