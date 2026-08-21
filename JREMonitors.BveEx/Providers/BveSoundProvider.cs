using System;
using System.Collections.Generic;
using BveEx.Extensions.SoundFactory;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.Core.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BveSoundProvider : ISoundProvider
    {
        private Dictionary<string, Sound> _sounds;

        public BveSoundProvider(Dictionary<string, Sound> sounds)
        {
            _sounds = sounds;
        }

        public void PlaySound(string soundName, float volume)
        {
            if (!_sounds.TryGetValue(soundName, out var sound)) return;
            sound.Play(volume, 1, 0);
        }

        public void Reconfigure(ISoundFactory soundFactory, IReadOnlyDictionary<string, ConfigPath> soundPaths)
        {
            var newSounds = new Dictionary<string, Sound>();
            foreach (var pair in soundPaths)
                try
                {
                    var path = pair.Value.GetAbsolutePath();
                    newSounds[pair.Key] = soundFactory.LoadFrom(path, 1, Sound.SoundPosition.Cab);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"Sound path '{pair.Value}' is invalid.");
                }

            _sounds = newSounds;
        }
    }
}