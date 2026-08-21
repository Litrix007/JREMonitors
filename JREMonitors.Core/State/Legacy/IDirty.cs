namespace JREMonitors.Core.State.Legacy
{
    public interface IDirty
    {
        bool IsDirty { get; }
        void MarkDirty();
        void ClearDirty();
    }
}