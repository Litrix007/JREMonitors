using System;
using Vortice.Mathematics;

namespace JREMonitors.Core.Providers
{
    public class RandomDelayProvider : IDelayProvider, ITickUpdatable
    {
        private readonly float _maxDelay;
        private readonly float _minDelay;
        private readonly Random _random = new Random();
        private double _currentThreshold;
        private double _elapsedSeconds;

        public RandomDelayProvider(float minDelaySeconds, float maxDelaySeconds)
        {
            _minDelay = minDelaySeconds;
            _maxDelay = maxDelaySeconds;
            ResetThreshold();
        }

        public long TickCount { get; private set; }

        public void Update(TimeSpan elapsed)
        {
            _elapsedSeconds += elapsed.TotalSeconds;
            if (_elapsedSeconds < _currentThreshold) return;
            TickCount++;
            ResetThreshold();
        }

        private void ResetThreshold()
        {
            _currentThreshold = MathHelper.Lerp(_minDelay, _maxDelay, (float)_random.NextDouble());
            _elapsedSeconds = 0;
        }
    }
}