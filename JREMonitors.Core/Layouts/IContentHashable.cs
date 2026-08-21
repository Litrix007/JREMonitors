namespace JREMonitors.Core.Layouts
{
    public interface IContentHashable
    {
        IContentSnapshot ToSnapshot();
    }

    public interface IContentSnapshot
    {
        int ContentHash { get; }
        bool Equals(IContentSnapshot other);
    }
}