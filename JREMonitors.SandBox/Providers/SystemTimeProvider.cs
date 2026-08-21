using System;
using System.Diagnostics;
using JREMonitors.Core.Providers;

namespace JREMonitors.SandBox.Providers
{
    public class SystemTimeProvider : ITimeProvider, ITickUpdatable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _first = true;

        public TimeSpan Elapsed { get; private set; }

        public void Update(TimeSpan elapsed)
        {
            CurrentTime = DateTime.Now.TimeOfDay;
            if (_first)
            {
                _first = false;
                _stopwatch.Restart();
                return;
            }

            Elapsed = _stopwatch.Elapsed;
            _stopwatch.Restart();
        }

        public TimeSpan CurrentTime { get; private set; }

        public void Reset()
        {
            _first = true;
            Elapsed = TimeSpan.Zero;
        }
    }
}