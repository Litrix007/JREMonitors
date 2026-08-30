using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;

namespace JREMonitors.E233.TidScreen.LampGroups
{
    public class TidTascWideLampGroup : TidLampGroupBase<TascViewModel>
    {
        private readonly Lamp _inchingActivatedLamp;
        private readonly Lamp _platformDecouplingLamp;
        private readonly Lamp _platformDoorAllClosedLamp;
        private readonly Lamp _platformInterlockingLamp;
        private readonly Lamp _tascBrakeLamp;
        private readonly Lamp _tascFailureLamp;
        private readonly Lamp _tascFixedDistanceLamp;
        private readonly Lamp _tascPatternLamp;
        private readonly Lamp _tascPowerLamp;
        private readonly Lamp _tascTurnOffLamp;
        private readonly Lamp _vehicleDoorAllClosedLamp;

        public TidTascWideLampGroup(RenderContext context, float y, float verticalAlignment) : base(context)
        {
            ViewModel = new TascViewModel();
            _tascPowerLamp = new Lamp(context,
                CreateTextLayout("TASC\n電源", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _tascPowerLamp.On.Bind(ViewModel.IsTascPowerLit);
            _tascPatternLamp = new Lamp(context,
                CreateTextLayout("TASC\nパターン", TidScreens.WideLampFontSize, new float[] { -2 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _tascPatternLamp.On.Bind(ViewModel.IsTascPatternLit);
            _tascBrakeLamp = new Lamp(context,
                CreateTextLayout("TASC\nブレーキ", TidScreens.WideLampFontSize, new float[] { -2 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow);
            _tascBrakeLamp.On.Bind(ViewModel.IsTascBrakeLit);
            _tascTurnOffLamp = new Lamp(context,
                CreateTextLayout("TASC\n切", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow);
            _tascTurnOffLamp.On.Bind(ViewModel.IsTascTurnOffLit);
            _tascFailureLamp = new Lamp(context,
                CreateTextLayout("TASC\n故障", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red);
            _tascFailureLamp.On.Bind(ViewModel.IsTascFailureLit);
            _inchingActivatedLamp = new Lamp(context,
                CreateTextLayout("インチング\n制御中", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow);
            _inchingActivatedLamp.On.Bind(ViewModel.IsInchingActivatedLit);
            var row1 = new Row(context, widgetSpacing: TidScreens.WideLampSpacing, widgets: new Widget[]
            {
                _tascPowerLamp, _tascPatternLamp, _tascBrakeLamp, _tascTurnOffLamp, _tascFailureLamp,
                _inchingActivatedLamp
            }, positionSnapToPixels: true);
            _tascFixedDistanceLamp = new Lamp(context,
                CreateTextLayout("定位置", TidScreens.WideLampFontSize, new float[] { -3 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _tascFixedDistanceLamp.On.Bind(ViewModel.IsTascFixedDistanceLit);
            _vehicleDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("車両ドア\n全閉", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _vehicleDoorAllClosedLamp.On.Bind(ViewModel.IsVehicleDoorAllClosedLit);
            _platformDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n全閉", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _platformDoorAllClosedLamp.On.Bind(ViewModel.IsPlatformDoorAllClosedLit);
            _platformInterlockingLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n連携", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _platformInterlockingLamp.On.Bind(ViewModel.IsPlatformInterlockingLit);
            _platformDecouplingLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n分離", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen);
            _platformDecouplingLamp.On.Bind(ViewModel.IsPlatformDecouplingLit);
            var row2 = new Row(context, widgetSpacing: TidScreens.WideLampSpacing, widgets: new Widget[]
            {
                _tascFixedDistanceLamp, _vehicleDoorAllClosedLamp, _platformDoorAllClosedLamp,
                _platformInterlockingLamp, _platformDecouplingLamp, new PlaceHolder(context)
            }, positionSnapToPixels: true);
            var col = new Col(context, AnchorX, y, TidScreens.WideLampWidth, TidScreens.WideLampHeight,
                widgetSpacing: TidScreens.WideRowSpacing, widgets: new Widget[] { row1, row2 },
                colHorizontalAlignment: 0.5f, positionSnapToPixels: true, colVerticalAlignment: verticalAlignment);
            AddChild(col);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}