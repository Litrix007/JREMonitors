using System;
using System.Collections.Generic;
using BveEx.Extensions.SoundFactory;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.Core.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BveSoundProvider : ISoundProvider, IDisposable
    {
        private readonly ISoundFactory _soundFactory;
        private Dictionary<string, Sound> _sounds;

        public BveSoundProvider(ISoundFactory soundFactory, IReadOnlyDictionary<string, ConfigPath> soundPaths)
        {
            _soundFactory = soundFactory;
            LoadSounds(soundPaths);
        }

        public void PlaySound(string soundName, float volume)
        {
            if (!_sounds.TryGetValue(soundName, out var sound)) return;
            sound.Play(volume, 1, 0);
        }

        private static void Clear(Dictionary<string, Sound> sounds)
        {
            if (sounds == null) return;
            foreach (var sound in sounds.Values)
            {
                sound.Dispose();
            }

            sounds.Clear();
        }

        public void Reconfigure(IReadOnlyDictionary<string, ConfigPath> soundPaths)
        {
            LoadSounds(soundPaths);
        }

        private void LoadSounds(IReadOnlyDictionary<string, ConfigPath> soundPaths)
        {
            var newSounds = new Dictionary<string, Sound>();
            foreach (var pair in soundPaths)
                try
                {
                    var path = pair.Value.GetAbsolutePath();
                    newSounds[pair.Key] = _soundFactory.LoadFrom(path, 1, Sound.SoundPosition.Cab);
                }
                catch (Exception)
                {
                    Clear(newSounds);
                    throw new InvalidOperationException($"Sound path '{pair.Value.Value}' is invalid.");
                }

            Clear(_sounds);
            _sounds = newSounds;
        }

        public void Dispose()
        {
            Clear(_sounds);
            _sounds = null;
        }
    }
}