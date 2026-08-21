using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;
using Vortice.Direct2D1;

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

        public TidTascWideLampGroup(RenderContext context, float anchorY, float verticalAlignment) : base(context)
        {
            ViewModel = new TascViewModel();
            var offsetY = -(TidScreens.WideRowHeight * 2 + TidScreens.WideRowSpacing) * verticalAlignment;
            _tascPowerLamp = new Lamp(context,
                CreateTextLayout("TASC\n電源", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascPowerLamp.On.Bind(ViewModel.IsTascPowerLit);
            _tascPatternLamp = new Lamp(context,
                CreateTextLayout("TASC\nパターン", TidScreens.WideLampFontSize, new float[] { -2 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascPatternLamp.On.Bind(ViewModel.IsTascPatternLit);
            _tascBrakeLamp = new Lamp(context,
                CreateTextLayout("TASC\nブレーキ", TidScreens.WideLampFontSize, new float[] { -2 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascBrakeLamp.On.Bind(ViewModel.IsTascBrakeLit);
            _tascTurnOffLamp = new Lamp(context,
                CreateTextLayout("TASC\n切", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascTurnOffLamp.On.Bind(ViewModel.IsTascTurnOffLit);
            _tascFailureLamp = new Lamp(context,
                CreateTextLayout("TASC\n故障", TidScreens.WideLampFontSize, new float[] { -1 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascFailureLamp.On.Bind(ViewModel.IsTascFailureLit);
            _inchingActivatedLamp = new Lamp(context,
                CreateTextLayout("インチング\n制御中", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _inchingActivatedLamp.On.Bind(ViewModel.IsInchingActivatedLit);
            var row1Bounds = new RectangleF(AnchorX, anchorY + offsetY, TidScreens.WideRowWidth,
                TidScreens.WideRowHeight);
            var row1 = Row.FromBounds(context, row1Bounds, TidScreens.WideLampSpacing, new Widget[]
            {
                _tascPowerLamp, _tascPatternLamp, _tascBrakeLamp, _tascTurnOffLamp, _tascFailureLamp,
                _inchingActivatedLamp
            }, 0.5f);
            AddChild(row1);
            _tascFixedDistanceLamp = new Lamp(context,
                CreateTextLayout("定位置", TidScreens.WideLampFontSize, new float[] { -3 }),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _tascFixedDistanceLamp.On.Bind(ViewModel.IsTascFixedDistanceLit);
            _vehicleDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("車両ドア\n全閉", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _vehicleDoorAllClosedLamp.On.Bind(ViewModel.IsVehicleDoorAllClosedLit);
            _platformDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n全閉", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _platformDoorAllClosedLamp.On.Bind(ViewModel.IsPlatformDoorAllClosedLit);
            _platformInterlockingLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n連携", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _platformInterlockingLamp.On.Bind(ViewModel.IsPlatformInterlockingLit);
            _platformDecouplingLamp = new Lamp(context,
                CreateTextLayout("ホームドア\n分離", TidScreens.WideLampFontSize),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: TidScreens.LampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                overrideTextInterpolationMode: InterpolationMode.Cubic);
            _platformDecouplingLamp.On.Bind(ViewModel.IsPlatformDecouplingLit);
            var row2Bounds = new RectangleF(AnchorX,
                anchorY + offsetY + TidScreens.WideRowHeight + TidScreens.WideRowSpacing,
                TidScreens.WideRowWidth,
                TidScreens.WideRowHeight);
            var row2 = Row.FromBounds(context, row2Bounds, TidScreens.WideLampSpacing, new Widget[]
            {
                _tascFixedDistanceLamp, _vehicleDoorAllClosedLamp, _platformDoorAllClosedLamp,
                _platformInterlockingLamp, _platformDecouplingLamp, new PlaceHolder(context)
            }, 0.5f);
            AddChild(row2);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}