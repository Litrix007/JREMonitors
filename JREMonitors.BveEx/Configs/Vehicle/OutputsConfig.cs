using System.Collections.Generic;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class OutputsConfig
    {
        private Dictionary<string, ConfigPath> _sound = new Dictionary<string, ConfigPath>();

        /// <summary>
        ///     音效输出配置，支持的值随车型变化。
        ///     <br />键为该输出类型的名称，值为音效文件路径。
        /// </summary>
        public Dictionary<string, ConfigPath> Sound
        {
            get => _sound;
            set => _sound = value ?? new Dictionary<string, ConfigPath>();
        }
    }
}