using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;

namespace JREMonitors.E233.TidScreen.LampGroups
{
    public class TidTascCompactLampGroup : TidLampGroupBase<TascViewModel>
    {
        private readonly Lamp _platformDecouplingLamp;
        private readonly Lamp _platformDoorAllClosedLamp;
        private readonly Lamp _platformDoorCutoutLamp;
        private readonly Lamp _platformInterlockingLamp;
        private readonly Lamp _tascBrakeLamp;
        private readonly Lamp _tascFailureLamp;
        private readonly Lamp _tascFixedDistanceLamp;
        private readonly Lamp _tascHoldingBrakeLamp;
        private readonly Lamp _tascPatternLamp;
        private readonly Lamp _tascPowerLamp;
        private readonly Lamp _tascTurnOffLamp;
        private readonly Lamp _vehicleDoorAllClosedLamp;

        public TidTascCompactLampGroup(RenderContext context, float anchorY, float rowSpacing,
            float verticalAlignment) :
            base(context)
        {
            ViewModel = new TascViewModel();
            const float fontSize = TidScreens.CompactLampFontSize;
            const float maxTextHeight = fontSize * 6;
            var lampWidth = LayoutLength.Absolute(TidScreens.CompactLampWidth);
            var lampHeight = LayoutLength.Absolute(TidScreens.CompactLampHeight);
            var offsetY = -(rowSpacing + lampHeight.AbsoluteValue * 2) * verticalAlignment;
            _tascPowerLamp = new Lamp(context,
                CreateTextLayout("TASC電源", fontSize),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascPowerLamp.On.Bind(ViewModel.IsTascPowerLit);
            _tascPatternLamp = new Lamp(context,
                CreateTextLayout("TASCパターン", fontSize, scaleY: maxTextHeight / fontSize / 8),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascPatternLamp.On.Bind(ViewModel.IsTascPatternLit);
            _tascBrakeLamp = new Lamp(context,
                CreateTextLayout("TASCブレーキ", fontSize, scaleY: maxTextHeight / fontSize / 8),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascBrakeLamp.On.Bind(ViewModel.IsTascBrakeLit);
            _tascTurnOffLamp = new Lamp(context,
                CreateTextLayout("TASC切", fontSize),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascTurnOffLamp.On.Bind(ViewModel.IsTascTurnOffLit);
            _tascFailureLamp = new Lamp(context,
                CreateTextLayout("TASC故障", fontSize),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascFailureLamp.On.Bind(ViewModel.IsTascFailureLit);
            var row1 = new Row(context, AnchorX, anchorY + offsetY, 0, 0, 25,
                new Widget[]
                {
                    _tascPowerLamp,
                    _tascPatternLamp,
                    _tascBrakeLamp,
                    _tascTurnOffLamp,
                    _tascFailureLamp,
                    new PlaceHolder(context, lampWidth, lampHeight),
                    new PlaceHolder(context, lampWidth, lampHeight),
                    new PlaceHolder(context, LayoutLength.Absolute(10), lampHeight),
                    new PlaceHolder(context, lampWidth, lampHeight),
                    new PlaceHolder(context, lampWidth, lampHeight)
                },
                0.5f, positionSnapToPixels: true);
            AddChild(row1);
            _tascHoldingBrakeLamp = new Lamp(context,
                CreateTextLayout("転動防止\nブレーキ", fontSize, sizeLimit: fontSize * 6, step: true),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascHoldingBrakeLamp.On.Bind(ViewModel.IsTascHoldingBrakeLit);
            _tascFixedDistanceLamp = new Lamp(context,
                CreateTextLayout("定\u3000位\u3000置", fontSize),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _tascFixedDistanceLamp.On.Bind(ViewModel.IsTascFixedDistanceLit);
            _vehicleDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("車両ドア全閉", fontSize),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _vehicleDoorAllClosedLamp.On.Bind(ViewModel.IsVehicleDoorAllClosedLit);
            _platformDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("ホームドア全閉", fontSize, scaleY: maxTextHeight / fontSize / 7),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _platformDoorAllClosedLamp.On.Bind(ViewModel.IsPlatformDoorAllClosedLit);
            _platformInterlockingLamp = new Lamp(context,
                CreateTextLayout("ホームドア連携", fontSize, scaleY: maxTextHeight / fontSize / 7),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _platformInterlockingLamp.On.Bind(ViewModel.IsPlatformInterlockingLit);
            _platformDecouplingLamp = new Lamp(context,
                CreateTextLayout("ホームドア分離", fontSize, scaleY: maxTextHeight / fontSize / 7),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _platformDecouplingLamp.On.Bind(ViewModel.IsPlatformDecouplingLit);
            _platformDoorCutoutLamp = new Lamp(context,
                CreateTextLayout("ホームドア開放", fontSize,
                    scaleY: maxTextHeight / fontSize / 7),
                preferredWidth: lampWidth,
                preferredHeight: lampHeight,
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                onBorderRadius: TidScreens.LampOnBorderRadius);
            _platformDoorCutoutLamp.On.Bind(ViewModel.IsPlatformDoorCutoutLit);
            var row2 = new Row(context, AnchorX, anchorY + offsetY + lampHeight.AbsoluteValue + rowSpacing, 0,
                0, 25, new Widget[]
                {
                    _tascHoldingBrakeLamp,
                    _tascFixedDistanceLamp,
                    _vehicleDoorAllClosedLamp,
                    _platformDoorAllClosedLamp,
                    _platformInterlockingLamp,
                    _platformDecouplingLamp,
                    _platformDoorCutoutLamp,
                    new PlaceHolder(context, LayoutLength.Absolute(10), lampHeight),
                    new PlaceHolder(context, lampWidth, lampHeight),
                    new PlaceHolder(context, lampWidth, lampHeight)
                },
                0.5f, positionSnapToPixels: true);
            AddChild(row2);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}