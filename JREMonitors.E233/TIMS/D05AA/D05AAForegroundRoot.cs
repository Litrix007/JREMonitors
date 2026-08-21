using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AA
{
    public class D05AAForegroundRoot : TIMSCommonForegroundRoot<D05AAForegroundRootViewModel>
    {
        public D05AAForegroundRoot(RenderContext context, TIMSVehicleSpec spec, D05AABrakeInformation brakeInformation)
            : base(context, ScreenIds.D05AA, "ブレーキ確認情報", new D05AABackground(context))
        {
            ViewModel = new D05AAForegroundRootViewModel(spec);
            var buttonGroup = new D05AAButtonGroup(context);
            AddChild(buttonGroup);
            var carGroup = new TIMSCarGroup(context, spec, false);
            carGroup.LeftSpacing.Bind(CreateComputed<float>(() =>
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
            carGroup.AnchorY.Value = 110;
            AddChild(carGroup);
            brakeInformation.FirstCarX.Bind(carGroup.FirstCarX);
            AddChild(brakeInformation);
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, ViewModel.FormationSpec);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D05AAForegroundRootViewModel : TIMSCommonForegroundRootViewModel
    {
        public D05AAForegroundRootViewModel(TIMSVehicleSpec spec)
        {
            var formationViewModel = new TIMSFormationViewModel(spec);
            FormationSpec = formationViewModel.FormationSpec;
            AddSubViewModel(formationViewModel);
        }

        public Signal<TIMSFormationSpec> FormationSpec { get; }
    }
}