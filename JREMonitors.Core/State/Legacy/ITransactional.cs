namespace JREMonitors.Core.State.Legacy
{
    public interface ITransactional
    {
        void BeginChange();
        void EndChange();
    }
}