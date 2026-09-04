using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABForegroundRoot : TIMSCommonForegroundRoot<D05ABForegroundRootViewModel>
    {
        public D05ABForegroundRoot(RenderContext context, TIMSVehicleSpec spec, D05ABDataTable dataTable)
            : base(context, ScreenIds.D05AB, "ブレーキ確認情報", new D05ABBackground(context))
        {
            ViewModel = new D05ABForegroundRootViewModel(spec);
            var buttonGroup = new D05ABButtonGroup(context);
            AddChild(buttonGroup);
            var carGroup = new TIMSCarGroup(context, spec, true, 95);
            for (var i = 0; i < carGroup.AllCars.Length; i++)
            {
                var car = carGroup.AllCars[i];
                car.PreferredWidth.Bind(CreateComputed(() =>
                {
                    var formationSpec = ViewModel.FormationSpec.Value;
                    return formationSpec != null && formationSpec.CarCount < 10 &&
                           (car.CarType == TIMSCarType.FirstCar || car.CarType == TIMSCarType.LastCar)
                        ? new LayoutLength(1, 2)
                        : LayoutLength.Flex();
                }));
            }

            carGroup.AnchorY.Value = 110;
            AddChild(carGroup);
            var notchInformation = new D05ABNotchInformation(context, spec, 160);
            notchInformation.X.Bind(carGroup.FirstCarX);
            AddChild(notchInformation);
            dataTable.FirstCarX.Bind(carGroup.FirstCarX);
            AddChild(dataTable);
            WatchEffect(EffectPhase.State, () =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (IsOffScreen) return;
                if (!IsFirstUpdate) Context.DisplayController.RequestReset();
                if (formationSpec == null) return;
                Invalidate(DirtyType.Layout);
            });
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D05ABForegroundRootViewModel : TIMSCommonForegroundRootViewModel
    {
        public D05ABForegroundRootViewModel(TIMSVehicleSpec spec)
        {
            var formationViewModel = new TIMSFormationViewModel(spec);
            FormationSpec = formationViewModel.FormationSpec;
            AddSubViewModel(formationViewModel);
        }

        public Signal<TIMSFormationSpec> FormationSpec { get; }
    }
}