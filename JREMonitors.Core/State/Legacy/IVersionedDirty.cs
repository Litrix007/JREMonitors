namespace JREMonitors.Core.State.Legacy
{
    public interface IVersionedDirty : IDirty
    {
        ulong Version { get; }
    }
}