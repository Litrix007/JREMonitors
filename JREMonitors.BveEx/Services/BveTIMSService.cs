using System;
using JREMonitors.BveEx.Providers;
using JREMonitors.Core.State;
using JREMonitors.E233.TIMS;

namespace JREMonitors.BveEx.Services
{
    public class BveTIMSService : TIMSService, IJumpStationListener
    {
        private bool _firstUpdate = true;
        private bool _jumping;
        private bool _shouldForceInstant;

        public BveTIMSService(DataHub dataHub, TIMSVehicleDirection vehicleDirection, float? baseInteriorTemperature,
            float? externalTemperature, float? baseHumidity) : base(dataHub, vehicleDirection, baseInteriorTemperature,
            externalTemperature, baseHumidity)
        {
        }

        public void OnJumpStation()
        {
            _jumping = true;
        }

        public override void Reconfigure(
            TIMSVehicleDirection vehicleDirection,
            float? baseInteriorTemperature,
            float? externalTemperature,
            float? baseHumidity)
        {
            base.Reconfigure(vehicleDirection, baseInteriorTemperature, externalTemperature, baseHumidity);
            _shouldForceInstant = true;
        }

        public override void Update(TimeSpan elapsed)
        {
            if (_jumping)
            {
                _jumping = false;
                _shouldForceInstant = true;
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