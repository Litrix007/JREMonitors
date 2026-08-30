using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;

namespace JREMonitors.E233.TidScreen.LampGroups
{
    public class TidNormalAtcLampGroup : TidLampGroupBase<NormalAtcViewModel>
    {
        private readonly Lamp _atc6EnabledLamp;
        private readonly Lamp _atcCutoutLamp;
        private readonly Lamp _atcEmergencyBrakeLamp;
        private readonly Lamp _atcPowerLamp;

        private readonly Lamp _atcServiceBrakeLamp;
        private readonly Lamp _atcTurnOffLamp;
        private readonly Lamp _atsSActivatedLamp;
        private readonly Lamp _atsSPowerLamp;
        private readonly Lamp _datcEnabledLamp;
        private readonly Lamp _emergencyRunLamp;
        private readonly Lamp _overrunActionLamp;
        private readonly Lamp _patternClearedLamp;

        public TidNormalAtcLampGroup(RenderContext context, float y, bool showAtsLamps) :
            base(context)
        {
            ViewModel = new NormalAtcViewModel();
            var row1Children = new List<Widget>();
            _datcEnabledLamp = new Lamp(context,
                CreateTextLayout("デジタル\nATC", TidScreens.WideLampFontSize, new float[] { 0, -2 }),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _datcEnabledLamp.On.Bind(ViewModel.IsDatcEnabledLit);
            row1Children.Add(_datcEnabledLamp);
            _atc6EnabledLamp = new Lamp(context, CreateTextLayout("ATC", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atc6EnabledLamp.On.Bind(ViewModel.IsAtc6EnabledLit);
            row1Children.Add(_atc6EnabledLamp);
            _atcTurnOffLamp = new Lamp(context, CreateTextLayout("切", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atcTurnOffLamp.On.Bind(ViewModel.IsAtcTurnOffLit);
            row1Children.Add(_atcTurnOffLamp);
            if (showAtsLamps)
            {
                _atsSPowerLamp = new Lamp(context, CreateTextLayout("ATS 電源", TidScreens.WideLampFontSize),
                    offBackgroundColor: TidScreens.LampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Yellow,
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex());
                _atsSPowerLamp.On.Bind(ViewModel.IsAtsSPowerLit);
                row1Children.Add(_atsSPowerLamp);
            }
            else
            {
                row1Children.Add(new PlaceHolder(context));
            }

            _patternClearedLamp = new Lamp(context,
                CreateTextLayout("パターン\n低滅", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _patternClearedLamp.On.Bind(ViewModel.IsPatternClearedLit);
            row1Children.Add(_patternClearedLamp);
            _emergencyRunLamp = new Lamp(context, CreateTextLayout("非常運転", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _emergencyRunLamp.On.Bind(ViewModel.IsEmergencyRunLit);
            row1Children.Add(_emergencyRunLamp);
            var row1 = new Row(context, widgetSpacing: TidScreens.WideLampSpacing, widgets: row1Children,
                positionSnapToPixels: true);
            var row2Children = new List<Widget>();
            _atcServiceBrakeLamp = new Lamp(context, CreateTextLayout("ATC 常用", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atcServiceBrakeLamp.On.Bind(ViewModel.IsAtcServiceBrakeLit);
            row2Children.Add(_atcServiceBrakeLamp);
            _atcEmergencyBrakeLamp = new Lamp(context, CreateTextLayout("ATC 非常", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atcEmergencyBrakeLamp.On.Bind(ViewModel.IsAtcEmergencyBrakeLit);
            row2Children.Add(_atcEmergencyBrakeLamp);
            _overrunActionLamp = new Lamp(context, CreateTextLayout("停通防止\n動作", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _overrunActionLamp.On.Bind(ViewModel.IsOverrunActionLit);
            row2Children.Add(_overrunActionLamp);
            if (showAtsLamps)
            {
                _atsSActivatedLamp = new Lamp(context, CreateTextLayout("ATS 動作", TidScreens.WideLampFontSize),
                    offBackgroundColor: TidScreens.LampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Red,
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex());
                _atsSActivatedLamp.On.Bind(ViewModel.IsAtsSActivatedLit);
                row2Children.Add(_atsSActivatedLamp);
            }
            else
            {
                row2Children.Add(new PlaceHolder(context));
            }

            _atcPowerLamp = new Lamp(context, CreateTextLayout("ATC 電源", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atcPowerLamp.On.Bind(ViewModel.IsAtcPowerLit);
            row2Children.Add(_atcPowerLamp);
            _atcCutoutLamp = new Lamp(context, CreateTextLayout("ATC 開放", TidScreens.WideLampFontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex());
            _atcCutoutLamp.On.Bind(ViewModel.IsAtcCutoutLit);
            row2Children.Add(_atcCutoutLamp);
            var row2 = new Row(context, widgetSpacing: TidScreens.WideLampSpacing, widgets: row2Children,
                positionSnapToPixels: true);
            var col = new Col(context, AnchorX, y, TidScreens.WideLampWidth, TidScreens.WideLampHeight,
                widgetSpacing: TidScreens.WideRowSpacing, widgets: new Widget[] { row1, row2 },
                colHorizontalAlignment: 0.5f, positionSnapToPixels: true);
            AddChild(col);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}