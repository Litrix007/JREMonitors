using JREMonitors.Core.Providers;
using Vortice.Mathematics;

namespace JREMonitors.Core.Contexts
{
    public class MonitorSoundController
    {
        private readonly ISoundProvider _soundProvider;
        private float _volume = 1;

        public MonitorSoundController(ISoundProvider soundProvider)
        {
            _soundProvider = soundProvider;
        }

        public float Volume
        {
            get => _volume;
            set => _volume = MathHelper.Clamp(value, 0, 1);
        }

        public void PlaySound(string soundName)
        {
            _soundProvider?.PlaySound(soundName, Volume);
        }
    }
}