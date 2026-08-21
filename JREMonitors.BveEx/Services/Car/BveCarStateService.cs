using System;
using JREMonitors.BveEx.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE.Services.Car;

namespace JREMonitors.BveEx.Services.Car
{
    public class BveCarStateService : CarStateService, IJumpStationListener
    {
        private bool _firstUpdate = true;
        private bool _jumping;
        private bool _shouldForceInstant;

        public BveCarStateService(DataHub dataHub) : base(dataHub)
        {
        }

        public void OnJumpStation()
        {
            if (!HasInitialized) return;
            _jumping = true;
        }

        public override void Update(TimeSpan elapsed)
        {
            if (HasInitialized && _jumping)
            {
                _jumping = false;
                _shouldForceInstant = true;
                return;
            }

            if (_firstUpdate || _shouldForceInstant)
            {
                _firstUpdate = false;
                _shouldForceInstant = false;
                ForceInstant();
            }

            base.Update(elapsed);
        }
    }
}