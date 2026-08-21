namespace JREMonitors.Core.Providers
{
    public interface IPanelDataProvider
    {
        int GetRawValue(string id);
        bool IsActive(string id);
    }
}