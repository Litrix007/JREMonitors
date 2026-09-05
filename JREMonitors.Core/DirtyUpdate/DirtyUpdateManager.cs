namespace JREMonitors.Core.DirtyUpdate
{
    /// <summary>
    ///     脏区管理器。
    /// </summary>
    public class DirtyUpdateManager : IDirtyReporter
    {
        public IDirtyReporter ActiveReporter { get; set; }

        public void ReportArea(DirtyArea area)
        {
            ActiveReporter?.ReportArea(area);
        }
    }
}