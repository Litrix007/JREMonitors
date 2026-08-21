using System;
using Vortice.Mathematics;

namespace JREMonitors.Core.Contexts
{
    public class DisplayController
    {
        private readonly Func<string> _activeScreenIdGetter;
        private float _brightness = 1;
        private bool _forceResetWhenChangingScreen;
        private string _screenIdToChange;
        private bool _shouldResetActiveScreen;

        public DisplayController(Func<string> activeScreenIdGetter)
        {
            _activeScreenIdGetter = activeScreenIdGetter;
        }

        public float Brightness
        {
            get => _brightness;
            set => _brightness = MathHelper.Clamp(value, 0f, 1f);
        }

        public string ActiveScreenId => _activeScreenIdGetter();

        public void RequestReset()
        {
            _shouldResetActiveScreen = true;
        }

        public bool ConsumeReset()
        {
            var shouldReset = _shouldResetActiveScreen;
            _shouldResetActiveScreen = false;
            return shouldReset;
        }

        public void RequestChangeScreen(string screenId, bool forceReset = false)
        {
            _screenIdToChange = screenId;
            _forceResetWhenChangingScreen = forceReset;
        }

        public bool ConsumeChangeScreen(out string screenId, out bool forceReset)
        {
            if (_screenIdToChange == null)
            {
                screenId = null;
                forceReset = false;
                return false;
            }

            screenId = _screenIdToChange;
            forceReset = _forceResetWhenChangingScreen;
            _screenIdToChange = null;
            _forceResetWhenChangingScreen = false;
            return true;
        }
    }
}