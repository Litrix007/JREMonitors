using JREMonitors.Core.Utils;

namespace JREMonitors.E233.Constants
{
    public static class DirectInputIds
    {
        public static readonly string CatenaryVoltage = nameof(CatenaryVoltage).ToCamelCase();

        #region Misc

        public static readonly string Series1Failure = nameof(Series1Failure).ToCamelCase();
        public static readonly string Series2Failure = nameof(Series2Failure).ToCamelCase();

        #endregion
    }
}