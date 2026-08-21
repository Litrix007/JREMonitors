namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class MonitorConfig
    {
        private CabProjectionConfig _cab = new CabProjectionConfig();
        private ExternalDisplayConfig _external = new ExternalDisplayConfig();

        /// <summary>
        ///     监视器原始画面分辨率，支持的值及默认值随车型变化。
        /// </summary>
        /// <remarks>
        ///     <para>调高可提升画面清晰度，但会增加GPU负载。</para>
        /// </remarks>
        public string Resolution { get; set; }

        /// <summary>
        ///     监视器在游戏驾驶室内的相关配置。
        /// </summary>
        public CabProjectionConfig Cab
        {
            get => _cab;
            set => _cab = value ?? new CabProjectionConfig();
        }

        /// <summary>
        ///     监视器关联的外置显示窗口相关配置。
        /// </summary>
        public ExternalDisplayConfig External
        {
            get => _external;
            set => _external = value ?? new ExternalDisplayConfig();
        }

        public void Validate()
        {
            if (Cab.Enabled) Cab.Validate();
            External.Validate();
        }
    }
}