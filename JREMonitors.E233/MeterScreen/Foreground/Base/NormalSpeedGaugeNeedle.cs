using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.Needles;
using JREMonitors.JRE.Providers;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class NormalSpeedGaugeNeedle : Widget<NormalSpeedGaugeNeedleViewModel>
    {
        private readonly GaugeNeedle _needle;

        public NormalSpeedGaugeNeedle(RenderContext context) : base(context)
        {
            ViewModel = new NormalSpeedGaugeNeedleViewModel();
            _needle = new GaugeNeedle(context, 755, 563,
                NormalSpeedGaugeBackground.SectorRadius + NormalSpeedGaugeBackground.TickMarkSpacing + 5,
                Shadows.GaugeNeedleInnerBig);
            _needle.Degree.Bind(CreateComputed(() => -210 + 240f * ViewModel.Speed / 160));
            AddChild(_needle);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class NormalSpeedGaugeNeedleViewModel : ViewModel
    {
        private DelayedSpeedProvider _delayedSpeedProvider;
        public Signal<int> Speed { get; } = new Signal<int>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _delayedSpeedProvider = dataHub.Get<DelayedSpeedProvider>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            Speed.Value = _delayedSpeedProvider.Speed;
        }
    }
}