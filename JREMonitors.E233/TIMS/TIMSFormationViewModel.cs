using System;
using JREMonitors.Core.Reactive;

namespace JREMonitors.E233.TIMS
{
    public class TIMSFormationViewModel : TIMSViewModel
    {
        protected readonly TIMSVehicleSpec Spec;

        public TIMSFormationViewModel(TIMSVehicleSpec spec)
        {
            Spec = spec;
        }

        public Signal<TIMSFormationSpec> FormationSpec { get; } = new Signal<TIMSFormationSpec>();

        protected bool IsFormationSpecChanged { get; private set; } = true;


        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);


            var formationSpec = Spec.GetFormationSpec(ICCardService.CurrentFormation);
            if (Signal<TIMSFormationSpec>.IsValueChanged(FormationSpec, formationSpec))
            {
                FormationSpec.Value = formationSpec;
                IsFormationSpecChanged = true;
            }
            else
            {
                IsFormationSpecChanged = false;
            }
        }

        protected override void OnReset()
        {
            base.OnReset();
            IsFormationSpecChanged = true;
        }
    }
}