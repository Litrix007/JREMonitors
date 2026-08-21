namespace JREMonitors.Core.Debugger
{
    public interface IDebugger
    {
        void Add(string text);
        void AddLine(string text);
        void AddLineLasting(string text);
        void ClearRight();
    }
}