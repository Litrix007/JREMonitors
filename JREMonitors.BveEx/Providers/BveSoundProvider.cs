using System;
using System.Collections.Generic;
using System.Reflection;
using BveEx.Extensions.SoundFactory;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;

namespace JREMonitors.BveEx.Providers
{
    public class BveSoundProvider : ISoundProvider, IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly MethodInfo EpMethodA =
            typeof(ep).GetMethod("a", Flags, null, new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly MethodInfo EpMethodB =
            typeof(ep).GetMethod("b", Flags, null, new[] { typeof(object), typeof(EventArgs) }, null);

        private readonly Scenario _scenario;
        private readonly ISoundFactory _soundFactory;
        private Dictionary<string, Sound> _sounds;

        public BveSoundProvider(DataHub dataHub, IReadOnlyDictionary<string, ConfigPath> soundPaths)
        {
            _scenario = dataHub.Get<Scenario>();
            _soundFactory = dataHub.Get<ISoundFactory>();
            LoadSounds(soundPaths);
        }

        public void Dispose()
        {
            Clear(_sounds);
            _sounds = null;
        }

        public void PlaySound(string soundName, float volume)
        {
            if (!_sounds.TryGetValue(soundName, out var sound)) return;
            sound.Play(volume, 1, 0);
        }

        private void Clear(Dictionary<string, Sound> sounds)
        {
            if (sounds == null) return;
            foreach (var sound in sounds.Values) DisposeSound(sound);

            sounds.Clear();
        }

        private void DisposeSound(Sound sound)
        {
            if (!(sound?.Src is ep epImpl)) return;
            TryUnsubscribeHandlers(epImpl);
            sound.Dispose();
        }

        private void TryUnsubscribeHandlers(ep epImpl)
        {
            try
            {
                if (!(_scenario.TimeManager.Src is cn timeManager)) return;
                var tickHandler = CreateHandler(epImpl, EpMethodA);
                timeManager.d(tickHandler);
                var stateHandler = CreateHandler(epImpl, EpMethodB);
                timeManager.h(stateHandler);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static EventHandler CreateHandler(ep target, MethodInfo method)
        {
            var handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), target, method);
            return handler;
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
    }
}