namespace JREMonitors.Core.Monitors
{
    public enum MonitorDrawState
    {
        /// <summary>
        ///     行扫描绘制背景。
        /// </summary>
        BackgroundDelayed,

        /// <summary>
        ///     绘制即时背景。
        /// </summary>
        BackgroundImmediate,

        /// <summary>
        ///     空闲。
        /// </summary>
        Idle,

        /// <summary>
        ///     绘制即时前景
        /// </summary>
        DrawImmediate,

        /// <summary>
        ///     离屏绘制行扫描前景。
        /// </summary>
        DrawDelayed,

        /// <summary>
        ///     同步行扫描前景内容。
        /// </summary>
        SyncDelayed
    }
}