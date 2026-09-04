using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.LampGroups
{
    public class MeterTascLampGroup : Widget<TascViewModel>
    {
        private const float SmallFontSize = 11;
        private readonly LampAdjacencyManager _lampAdjacencyManager;
        private readonly Lamp _platformDoorAllClosedLamp;
        private readonly Lamp _platformDoorCutoutLamp;
        private readonly Lamp _platformInterlockingLamp;
        private readonly Lamp _tascBrakeLamp;
        private readonly Lamp _tascFixedDistanceLamp;
        private readonly Lamp _tascHoldingBrakeLamp;
        private readonly Lamp _tascPatternLamp;
        private readonly Lamp _tascPowerLamp;
        private readonly Lamp _tascTurnOffLamp;
        private readonly Lamp _vehicleDoorAllClosedLamp;


        public MeterTascLampGroup(
            RenderContext context,
            RectangleF bounds,
            LampAdjacencyManager lampAdjacencyManager,
            float rowSpacing,
            float colSpacing,
            bool showExtraLamps,
            bool expandToFillGaps,
            float lampOffBorderRadius = 6,
            float lampOnBorderRadius = 6,
            float lampOnInnerShadowWidth = 3.5f
        ) : base(context)
        {
            ViewModel = new TascViewModel();
            _lampAdjacencyManager = lampAdjacencyManager;
            _tascPowerLamp = new Lamp(context,
                CreateTextLayout("ＴＡＳＣ\n電源"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _tascPowerLamp.On.Bind(ViewModel.IsTascPowerLit);
            _tascPatternLamp = new Lamp(context,
                CreateTextLayout("ＴＡＳＣ\nパターン"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _tascPatternLamp.On.Bind(ViewModel.IsTascPatternLit);
            _tascBrakeLamp = new Lamp(context,
                CreateTextLayout("ＴＡＳＣ\nブレーキ"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _tascBrakeLamp.On.Bind(ViewModel.IsTascBrakeLit);
            _tascTurnOffLamp = new Lamp(context,
                CreateTextLayout("ＴＡＳＣ\n切"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _tascTurnOffLamp.On.Bind(ViewModel.IsTascTurnOffLit);
            TascFailureLamp = new Lamp(context,
                CreateTextLayout("ＴＡＳＣ\n故障"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            TascFailureLamp.On.Bind(ViewModel.IsTascFailureLit);
            var row1Children = new List<Widget>();
            if (showExtraLamps)
            {
                row1Children.Add(null);
                row1Children.Add(null);
            }

            row1Children.Add(_tascPowerLamp);
            row1Children.Add(_tascPatternLamp);
            row1Children.Add(_tascBrakeLamp);
            row1Children.Add(_tascTurnOffLamp);
            row1Children.Add(TascFailureLamp);

            var row2Children = new List<Widget>();
            if (showExtraLamps)
            {
                _tascHoldingBrakeLamp = new Lamp(context,
                    CreateTextLayout("転動防止\nブレーキ"),
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(),
                    offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Yellow,
                    offBorderRadius: lampOffBorderRadius,
                    onBorderRadius: lampOnBorderRadius,
                    onInnerShadowWidth: lampOnInnerShadowWidth,
                    expandToFillGaps: expandToFillGaps);
                _tascHoldingBrakeLamp.On.Bind(ViewModel.IsTascHoldingBrakeLit);
                row2Children.Add(_tascHoldingBrakeLamp);
            }

            _tascFixedDistanceLamp = new Lamp(context,
                CreateTextLayout("定位置"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _tascFixedDistanceLamp.On.Bind(ViewModel.IsTascFixedDistanceLit);
            row2Children.Add(_tascFixedDistanceLamp);
            _vehicleDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout("車両ドア\n全閉"),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _vehicleDoorAllClosedLamp.On.Bind(ViewModel.IsVehicleDoorAllClosedLit);
            row2Children.Add(_vehicleDoorAllClosedLamp);
            _platformDoorAllClosedLamp = new Lamp(context,
                CreateTextLayout(new RichTextBuilder().Append("ホームドア", SmallFontSize).Append("\n全閉").Build()),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _platformDoorAllClosedLamp.On.Bind(ViewModel.IsPlatformDoorAllClosedLit);
            row2Children.Add(_platformDoorAllClosedLamp);
            _platformInterlockingLamp = new Lamp(context,
                CreateTextLayout(new RichTextBuilder().Append("ホームドア", SmallFontSize).Append("\n連携").Build()),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            _platformInterlockingLamp.On.Bind(ViewModel.IsPlatformInterlockingLit);
            row2Children.Add(_platformInterlockingLamp);
            PlatformDecouplingLamp = new Lamp(context,
                CreateTextLayout(new RichTextBuilder().Append("ホームドア", SmallFontSize).Append("\n分離").Build()),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.NormalGreen,
                offBorderRadius: lampOffBorderRadius,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: expandToFillGaps);
            PlatformDecouplingLamp.On.Bind(ViewModel.IsPlatformDecouplingLit);
            row2Children.Add(PlatformDecouplingLamp);
            if (showExtraLamps)
            {
                _platformDoorCutoutLamp = new Lamp(context,
                    CreateTextLayout(new RichTextBuilder().Append("ホームドア", SmallFontSize).Append("\n開放")
                        .Build()),
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(),
                    offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Red,
                    offBorderRadius: lampOffBorderRadius,
                    onBorderRadius: lampOnBorderRadius,
                    onInnerShadowWidth: lampOnInnerShadowWidth,
                    expandToFillGaps: expandToFillGaps);
                _platformDoorCutoutLamp.On.Bind(ViewModel.IsPlatformDoorCutoutLit);
                row2Children.Add(_platformDoorCutoutLamp);
            }

            var grid = new RowGrid(context, bounds, rowSpacing, colSpacing, new ICollection<Widget>[]
            {
                row1Children, row2Children
            }, positionSnapToPixels: true);
            lampAdjacencyManager?.AddFromRowGrid(grid, rowSpacing, colSpacing);
            AddChild(grid);
        }

        public Lamp TascFailureLamp { get; }
        public Lamp PlatformDecouplingLamp { get; }
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;


        private TextLayout CreateTextLayout(string text)
        {
            return new TextLayout(Context,
                Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, 12, fontWeight: FontWeight.DemiBold),
                text);
        }

        private TextLayout CreateTextLayout(RichTextDocument document)
        {
            return new TextLayout(Context,
                Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, 12, fontWeight: FontWeight.DemiBold),
                document);
        }
    }
}