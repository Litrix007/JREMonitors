namespace JREMonitors.BveEx.Configs.Runtime
{
    public class RuntimeConfig
    {
        private InfoAssistantConfig _infoAssistant = new InfoAssistantConfig();

        public InfoAssistantConfig InfoAssistant
        {
            get => _infoAssistant;
            set => _infoAssistant = value ?? new InfoAssistantConfig();
        }
    }
}