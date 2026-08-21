namespace JREMonitors.E233.TIMS.ICCard
{
    public enum TIMSStationTask
    {
        None = 0,

        /// <summary>
        ///     待
        /// </summary>
        Waiting,

        /// <summary>
        ///     整
        /// </summary>
        Adjustment,

        /// <summary>
        ///     分
        /// </summary>
        Decoupling,

        /// <summary>
        ///     併
        /// </summary>
        Coupling
    }
}