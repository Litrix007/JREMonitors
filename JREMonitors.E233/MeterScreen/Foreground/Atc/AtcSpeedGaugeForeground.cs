using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.Needles;
using JREMonitors.E233.ViewModels;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Foreground.Atc
{
    public class AtcSpeedGaugeForeground : Widget<AtcSpeedViewModel>
    {
        private readonly AtcAbsoluteStopCross _atcAbsoluteStopCross;
        private readonly Lamp _atcPatternApproachLamp;
        private readonly AtcPatternGauge _atcPatternGauge;
        private readonly Lamp _atcShuntLamp;
        private readonly GaugeNeedle _speedNeedle;

        public AtcSpeedGaugeForeground(RenderContext context, float speedOffsetY) : base(context)
        {
            ViewModel = new AtcSpeedViewModel();
            var y = 563 + speedOffsetY;
            _atcPatternApproachLamp = new Lamp(context,
                new TextLayout(context,
                    context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 18, fontWeight: FontWeight.Bold),
                    "パターン接近"), 755 - 104f / 2, 285, LayoutLength.Absolute(104), LayoutLength.Absolute(24),
                offBackgroundType: LampBackgroundType.Recessed, onBackgroundColor: MonitorColors.Yellow);
            _atcPatternApproachLamp.IsVisible.Bind(ViewModel.ShowAtcParts);
            AddChild(_atcPatternApproachLamp);
            _atcPatternGauge = new AtcPatternGauge(context, 755, y);
            _atcPatternGauge.IsVisible.Bind(ViewModel.ShowAtcParts);
            AddChild(_atcPatternGauge);
            _atcShuntLamp = new Lamp(context,
                new TextLayout(context,
                    context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 20, fontWeight: FontWeight.Bold),
                    "入換"),
                578, 717, LayoutLength.Absolute(60), LayoutLength.Absolute(40),
                offBackgroundType: LampBackgroundType.Recessed, onBackgroundColor: MonitorColors.Yellow);
            _atcShuntLamp.IsVisible.Bind(ViewModel.ShowAtcParts);
            AddChild(_atcShuntLamp);
            _atcAbsoluteStopCross = new AtcAbsoluteStopCross(context, 760, 737);
            _atcAbsoluteStopCross.IsVisible.Bind(ViewModel.ShowAtcParts);
            AddChild(_atcAbsoluteStopCross);
            _speedNeedle = new GaugeNeedle(context, 755, y,
                NormalSpeedGaugeBackground.SectorRadius + NormalSpeedGaugeBackground.TickMarkSpacing + 7,
                Shadows.GaugeNeedleInnerBig, 1 / 10.5f, trangleWidthRatio: 2.1f);
            _speedNeedle.Degree.Bind(CreateComputed(() => -216 + 252f * ViewModel.Speed / 140));
            AddChild(_speedNeedle);
            WatchEffect(EffectPhase.State, () =>
            {
                if (!ViewModel.ShowAtcParts) return;
                _atcPatternApproachLamp.On.Value = ViewModel.PatternApproach;
                _atcPatternGauge.TurnOff.Value = ViewModel.TurnOff;
                _atcPatternGauge.SpeedLimit.Value = ViewModel.SpeedLimit;
                _atcShuntLamp.On.Value = ViewModel.Shunt;
                _atcAbsoluteStopCross.Stopped.Value = ViewModel.AbsoluteStop;
            });
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}