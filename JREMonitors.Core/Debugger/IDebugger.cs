namespace JREMonitors.Core.Debugger
{
    public interface IDebugger
    {
        /// <summary>
        ///     添加日志文本，在下一帧会被清除。
        /// </summary>
        void Add(string text);

        /// <summary>
        ///     添加日志文本并换行，在下一帧会被清除。
        /// </summary>
        void AddLine(string text);

        /// <summary>
        ///     添加日志文本并换行，不会在下一帧被清除。
        /// </summary>
        void AddLineLasting(string text);
    }
}