using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen.LampGroups
{
    public class TidAtsLampGroup : TidLampGroupBase<AtsStateViewModel>
    {
        private readonly Lamp _atsPBrakeCutoutLamp;
        private readonly Lamp _atsPEmergencyBrakeLamp;
        private readonly Lamp _atsPEnabledLamp;
        private readonly Lamp _atsPFailureLamp;
        private readonly Lamp _atsPPatternApproachLamp;
        private readonly Lamp _atsPPowerLamp;
        private readonly Lamp _atsPServiceBrakeLamp;
        private readonly Lamp _atsSActivatedLamp;
        private readonly Lamp _atsSPowerLamp;
        private readonly Divider _divider;
        private readonly bool _showAtsSLamps;

        public TidAtsLampGroup(RenderContext context, float y, bool showAtsLamps, bool centerAlign) : base(
            context)
        {
            ViewModel = new AtsStateViewModel();
            const float fontSize = TidScreens.CompactLampFontSize;
            var lampWidth = LayoutLength.Absolute(TidScreens.CompactLampWidth);
            var lampHeight = LayoutLength.Absolute(TidScreens.CompactLampHeight);
            _showAtsSLamps = showAtsLamps;
            float spacing = showAtsLamps ? 25 : 40;
            var lamps = new List<Widget>();
            _atsPPowerLamp = new Lamp(context,
                CreateTextLayout(centerAlign ? "P\u3000電\u3000源" : "P 電 源\u3000\u3000 ", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPPowerLamp.On.Bind(ViewModel.IsAtsPPowerLit);
            lamps.Add(_atsPPowerLamp);
            _atsPPatternApproachLamp = new Lamp(context,
                CreateTextLayout("パターン接近", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPPatternApproachLamp.On.Bind(ViewModel.IsAtsPPatternApproachLit);
            lamps.Add(_atsPPatternApproachLamp);
            _atsPServiceBrakeLamp = new Lamp(context,
                CreateTextLayout("ブレーキ動作", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPServiceBrakeLamp.On.Bind(ViewModel.IsAtsPServiceBrakeLit);
            lamps.Add(_atsPServiceBrakeLamp);
            _atsPEmergencyBrakeLamp = new Lamp(context,
                CreateTextLayout("非常ブレーキ", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPEmergencyBrakeLamp.On.Bind(ViewModel.IsAtsPEmergencyBrakeLit);
            lamps.Add(_atsPEmergencyBrakeLamp);
            _atsPBrakeCutoutLamp = new Lamp(context,
                CreateTextLayout("ブレーキ開放", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPBrakeCutoutLamp.On.Bind(ViewModel.IsAtsPBrakeCutoutLit);
            lamps.Add(_atsPBrakeCutoutLamp);
            _atsPEnabledLamp = new Lamp(context,
                CreateTextLayout(centerAlign ? "ATSーP" : "ATSーP\u3000", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPEnabledLamp.On.Bind(ViewModel.IsAtsPEnabledLit);
            lamps.Add(_atsPEnabledLamp);
            _atsPFailureLamp = new Lamp(context,
                CreateTextLayout(centerAlign ? "故\u3000障" : "故\u3000障\u3000\u3000\u3000", fontSize),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _atsPFailureLamp.On.Bind(ViewModel.IsAtsPFailureLit);
            lamps.Add(_atsPFailureLamp);
            if (_showAtsSLamps)
            {
                _divider = new Divider(context, LayoutLength.Absolute(10), lampHeight, Colors.Gray,
                    refreshSpeed: () => _divider.IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null);
                lamps.Add(_divider);
                _atsSPowerLamp = new Lamp(context,
                    CreateTextLayout(centerAlign ? "ATS電源" : "ATS電源\u3000", fontSize),
                    offBackgroundColor: TidScreens.LampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.White,
                    preferredWidth: lampWidth,
                    preferredHeight: lampHeight,
                    onBorderRadius: TidScreens.LampOnBorderRadius);
                _atsSPowerLamp.On.Bind(ViewModel.IsAtsSPowerLit);
                lamps.Add(_atsSPowerLamp);
                _atsSActivatedLamp = new Lamp(context,
                    CreateTextLayout(centerAlign ? "ATS動作" : "ATS動作\u3000", fontSize),
                    offBackgroundColor: TidScreens.LampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Red,
                    preferredWidth: lampWidth,
                    preferredHeight: lampHeight,
                    onBorderRadius: TidScreens.LampOnBorderRadius);
                _atsSActivatedLamp.On.Bind(ViewModel.IsAtsSActivatedLit);
                lamps.Add(_atsSActivatedLamp);
            }

            var row = new Row(context, AnchorX, y, 0, 0, spacing, lamps, 0.5f, positionSnapToPixels: true);
            AddChild(row);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}