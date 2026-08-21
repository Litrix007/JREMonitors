using System;
using JREMonitors.BveEx.Providers;
using JREMonitors.Core.Services;

namespace JREMonitors.BveEx.Services
{
    public class BveBlockingService : BlockingService, IJumpStationListener
    {
        private bool _jumping;

        public void OnJumpStation()
        {
            _jumping = true;
        }

        public override void Update(TimeSpan elapsed)
        {
            if (_jumping)
            {
                _jumping = false;
                Clear();
                return;
            }

            base.Update(elapsed);
        }
    }
}