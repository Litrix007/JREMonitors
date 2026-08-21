using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.E233.Constants;
using Vortice;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AAForegroundRoot : TIMSCommonForegroundRoot<C01AXForegroundRootViewModel>
    {
        public C01AAForegroundRoot(RenderContext context, TIMSVehicleSpec spec) : this(context, spec,
            new C01AXBackground(context))
        {
        }

        private C01AAForegroundRoot(RenderContext context, TIMSVehicleSpec spec, C01AXBackground background) : base(
            context, ScreenIds.C01AA, "車掌情報", background)
        {
            ViewModel = new C01AXForegroundRootViewModel(spec);
            var buttonGroup = new C01AXButtonGroup(context, ScreenIds.C01AA);
            buttonGroup.FormationSpec.Bind(ViewModel.FormationSpec);
            AddChild(buttonGroup);
            AddChild(new C01AXBaseInfoGroup(context));
            var doorStateGroup = new TIMSDoorStateGroup(context, spec);
            doorStateGroup.Y.Bind(CreateComputed<float>(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec != null && formationSpec.HasGreenCar) return 100;

                return 110;
            }));
            doorStateGroup.LeftSpacing.Bind(CreateComputed<float>(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (spec.MaxFormationCarCount <= 10 && formationSpec != null)
                    if (formationSpec.CarCount == 10)
                        return 108;

                if (formationSpec != null && formationSpec.CarCount > 10) return 108;
                return 0;
            }));
            doorStateGroup.RightSpacing.Bind(CreateComputed<float>(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (spec.C01AASpec.CountPassengers && formationSpec != null && formationSpec.CarCount > 10) return 40;
                return 0;
            }));
            AddChild(doorStateGroup);
            var normalCarInfo = new C01AANormalCarInfo(context, spec);
            normalCarInfo.FirstCarX.Bind(doorStateGroup.FirstCarX);
            normalCarInfo.Y.Bind(CreateComputed(() => doorStateGroup.Y + 130));
            background.BlackBounds.Bind(CreateComputed(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec != null && formationSpec.HasGreenCar)
                    return new RectangleF(0, normalCarInfo.Y, 800, 220);

                return new RectangleF(0, normalCarInfo.Y, 800, 240);
            }));
            AddChild(normalCarInfo);
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, ViewModel.FormationSpec);
        }
    }

    public class C01ABForegroundRoot : TIMSCommonForegroundRoot<C01AXForegroundRootViewModel>
    {
        public C01ABForegroundRoot(RenderContext context, TIMSVehicleSpec spec) : this(context, spec,
            new C01AXBackground(context))
        {
        }

        private C01ABForegroundRoot(RenderContext context, TIMSVehicleSpec spec, C01AXBackground background) : base(
            context, ScreenIds.C01AB, "車掌情報", background)
        {
            ViewModel = new C01AXForegroundRootViewModel(spec);
            background.BlackBounds.Value = new RawRectF(0, 200, 800, 440);
            var buttonGroup = new C01AXButtonGroup(context, ScreenIds.C01AB);
            buttonGroup.FormationSpec.Bind(ViewModel.FormationSpec);
            AddChild(buttonGroup);
            AddChild(new C01AXBaseInfoGroup(context));
            AddChild(new C01ABGreenCarInfo(context, spec));
            WatchEffect(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (IsOffScreen) return;
                if (formationSpec != null && !formationSpec.HasGreenCar)
                    Context.DisplayController.RequestChangeScreen(ScreenIds.C01AA);
                else if (!IsFirstUpdate) Context.DisplayController.RequestReset();
            });
        }
    }

    public class C01AXForegroundRootViewModel : TIMSCommonForegroundRootViewModel
    {
        public C01AXForegroundRootViewModel(TIMSVehicleSpec spec)
        {
            var timsFormationViewModel = new TIMSFormationViewModel(spec);
            FormationSpec = timsFormationViewModel.FormationSpec;
            AddSubViewModel(timsFormationViewModel);
        }

        public Signal<TIMSFormationSpec> FormationSpec { get; }
    }
}