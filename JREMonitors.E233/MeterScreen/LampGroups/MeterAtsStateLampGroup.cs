using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;
using Vortice;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.LampGroups
{
    public class MeterAtsStateLampGroup : Widget<AtsStateViewModel>
    {
        private const float FontSize = 15;
        private const float FixedLineSpacing = -FontSize;
        private readonly Lamp _atsPBrakeCutoutLamp;
        private readonly Lamp _atsPEmergencyBrakeLamp;
        private readonly Lamp _atsPEnabledLamp;
        private readonly Lamp _atsPFailureLamp;
        private readonly Lamp _atsPPatternApproachLamp;
        private readonly Lamp _atsPPowerLamp;
        private readonly Lamp _atsPServiceBrakeLamp;
        private readonly Lamp _atsSActivatedLamp;
        private readonly Lamp _atsSPowerLamp;

        public MeterAtsStateLampGroup(
            RenderContext context,
            float lampSpacing,
            float lampOnBorderRadius,
            bool showAtsSLamps,
            IEnumerable<Widget> before = null
        ) : base(context)
        {
            ViewModel = new AtsStateViewModel();
            _atsPPowerLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "P  電  源", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse), preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen, onBorderRadius: lampOnBorderRadius);
            _atsPPowerLamp.On.Bind(ViewModel.IsAtsPPowerLit);
            _atsPPatternApproachLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "パターン\n接近", ContentOrientation.Vertical, arrangement: ContentArrangement.Step,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true, sizeLimit: 75,
                    fixedLineSpacing: FixedLineSpacing),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow, onBorderRadius: lampOnBorderRadius);
            _atsPPatternApproachLamp.On.Bind(ViewModel.IsAtsPPatternApproachLit);
            _atsPServiceBrakeLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "ブレーキ\n動作", ContentOrientation.Vertical, arrangement: ContentArrangement.Step,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true, sizeLimit: 75,
                    fixedLineSpacing: FixedLineSpacing),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow, onBorderRadius: lampOnBorderRadius);
            _atsPServiceBrakeLamp.On.Bind(ViewModel.IsAtsPServiceBrakeLit);
            _atsPEmergencyBrakeLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "非常\nブレーキ", ContentOrientation.Vertical, arrangement: ContentArrangement.Step,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true, sizeLimit: 75,
                    fixedLineSpacing: FixedLineSpacing),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red, onBorderRadius: lampOnBorderRadius);
            _atsPEmergencyBrakeLamp.On.Bind(ViewModel.IsAtsPEmergencyBrakeLit);
            _atsPBrakeCutoutLamp = new Lamp(context,
                new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "ブレーキ\n開放", ContentOrientation.Vertical, arrangement: ContentArrangement.Step,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true, sizeLimit: 75,
                    fixedLineSpacing: FixedLineSpacing),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow, onBorderRadius: lampOnBorderRadius);
            _atsPBrakeCutoutLamp.On.Bind(ViewModel.IsAtsPBrakeCutoutLit);
            _atsPEnabledLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "ATSーP", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse),
                preferredWidth: LayoutLength.Flex(), preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen, onBorderRadius: lampOnBorderRadius);
            _atsPEnabledLamp.On.Bind(ViewModel.IsAtsPEnabledLit);
            _atsPFailureLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                        fontWeight: FontWeight.Bold),
                    "故\u3000障", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse),
                preferredWidth: LayoutLength.Flex(), preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red, onBorderRadius: lampOnBorderRadius);
            _atsPFailureLamp.On.Bind(ViewModel.IsAtsPFailureLit);
            var lamps = before == null ? new List<Widget>() : before.ToList();
            lamps.AddRange(new Widget[]
            {
                _atsPPowerLamp, _atsPPatternApproachLamp, _atsPServiceBrakeLamp, _atsPEmergencyBrakeLamp,
                _atsPBrakeCutoutLamp, _atsPEnabledLamp, _atsPFailureLamp
            });
            if (showAtsSLamps)
            {
                var divider = new Divider(context, LayoutLength.Absolute(6), LayoutLength.Flex(), MonitorColors.White);
                _atsSPowerLamp = new Lamp(context, new TextLayout(Context,
                        Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                            fontWeight: FontWeight.Bold),
                        "ATS電源", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                        flowDirection: ContentFlowDirection.Reverse), preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(),
                    offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor, onBorderRadius: lampOnBorderRadius);
                _atsSPowerLamp.On.Bind(ViewModel.IsAtsSPowerLit);
                _atsSActivatedLamp = new Lamp(context, new TextLayout(Context,
                        Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, FontSize,
                            fontWeight: FontWeight.Bold),
                        "ATS動作", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                        flowDirection: ContentFlowDirection.Reverse), preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(), offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Red, onBorderRadius: lampOnBorderRadius);
                _atsSActivatedLamp.On.Bind(ViewModel.IsAtsSActivatedLit);
                lamps.Add(divider);
                lamps.Add(_atsSPowerLamp);
                lamps.Add(_atsSActivatedLamp);
            }

            var row = Row.FromBounds(context, new RawRectF(608, 6, 1016, 107), lampSpacing, lamps,
                widgetVerticalAlign: 0.5f);
            AddChild(row);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}