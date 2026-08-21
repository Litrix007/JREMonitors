namespace JREMonitors.Core.DirtyUpdate
{
    public class DirtyUpdateManager : IDirtyReporter
    {
        public IDirtyReporter ActiveReporter { get; set; }

        public void ReportArea(DirtyArea area)
        {
            ActiveReporter?.ReportArea(area);
        }
    }
}