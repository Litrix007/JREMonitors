using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D02AA
{
    public class D02AAForegroundRoot : TIMSCommonForegroundRoot<D02AAForegroundRootViewModel>
    {
        public D02AAForegroundRoot(RenderContext context, TIMSVehicleSpec spec) : base(context, ScreenIds.D02AA, "車両情報",
            new D02AABackground(context))
        {
            ViewModel = new D02AAForegroundRootViewModel(spec);
            var driverButtonGroup = new TIMSDriverButtonGroup(context, ScreenIds.D02AA);
            AddChild(driverButtonGroup);
            var doorStateGroup = new TIMSDoorStateGroup(context, spec, 110);
            doorStateGroup.LeftSpacing.Bind(CreateComputed<float>(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (spec.MaxFormationCarCount <= 10 && formationSpec != null)
                    if (formationSpec.CarCount == 10)
                        return 108;

                if (formationSpec != null)
                    if (formationSpec.CarCount > 10)
                        return 108;

                return 0;
            }));
            AddChild(doorStateGroup);
            var equipmentInformation = new D02AAEquipmentInformation(context, spec);
            equipmentInformation.FirstCarX.Bind(CreateComputed(() => doorStateGroup.FirstCarX.Value));
            AddChild(equipmentInformation);
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, ViewModel.FormationSpec);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D02AAForegroundRootViewModel : TIMSCommonForegroundRootViewModel
    {
        public D02AAForegroundRootViewModel(TIMSVehicleSpec spec)
        {
            var timsFormationViewModel = new TIMSFormationViewModel(spec);
            FormationSpec = timsFormationViewModel.FormationSpec;
            AddSubViewModel(timsFormationViewModel);
        }

        public Signal<TIMSFormationSpec> FormationSpec { get; }
    }
}