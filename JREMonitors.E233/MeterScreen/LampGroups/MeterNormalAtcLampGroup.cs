using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.ViewModels;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.LampGroups
{
    public class MeterNormalAtcLampGroup : Widget<NormalAtcViewModel>
    {
        private readonly Lamp _atc6EnabledLamp;
        private readonly Lamp _atcCutoutLamp;
        private readonly Lamp _atcEmergencyBrakeLamp;
        private readonly Lamp _atcPowerLamp;
        private readonly Lamp _atcTurnOffLamp;
        private readonly Lamp _atsSActivatedLamp;
        private readonly Lamp _atsSPowerLamp;

        private readonly Lamp _datcEnabledLamp;
        private readonly Lamp _emergencyRunLamp;

        private readonly float _fontSize;
        private readonly Lamp _inchingActivatedLamp;
        private readonly LampAdjacencyManager _lampAdjacencyManager;
        private readonly Lamp _overrunActionLamp;
        private readonly Lamp _patternClearedLamp;
        private readonly bool _showAtsSLamps;


        public MeterNormalAtcLampGroup(
            RenderContext context,
            RectangleF bounds,
            LampAdjacencyManager lampAdjacencyManager,
            float lampSpacing,
            float rowSpacing,
            float fontSize,
            InchingActivatedLampProperties inchingActivatedLampProperties,
            bool showAtsSLamps,
            float lampOnBorderRadius = 6,
            float lampOnInnerShadowWidth = 3.5f
        ) : base(context)
        {
            ViewModel = new NormalAtcViewModel();
            _lampAdjacencyManager = lampAdjacencyManager;
            _showAtsSLamps = showAtsSLamps;
            _fontSize = fontSize;
            _inchingActivatedLamp = new Lamp(context,
                CreateTextLayout(inchingActivatedLampProperties.Text, inchingActivatedLampProperties.SizeLimit,
                    inchingActivatedLampProperties.FontSize),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _inchingActivatedLamp.IsVisible.Bind(inchingActivatedLampProperties.IsVisible);
            _inchingActivatedLamp.SkipArrangeWhenHidden.Value = false;
            _inchingActivatedLamp.IncludeInTotalMajorDimensionSizeWhenHidden.Value = true;
            _inchingActivatedLamp.On.Bind(ViewModel.IsInchingActivatedLit);
            var row1Children = new List<Widget>
                { inchingActivatedLampProperties.IsBottom ? null : _inchingActivatedLamp };
            _datcEnabledLamp = new Lamp(context, CreateTextLayout("デジタルATC"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _datcEnabledLamp.On.Bind(ViewModel.IsDatcEnabledLit);
            row1Children.Add(_datcEnabledLamp);
            _atc6EnabledLamp = new Lamp(context, CreateTextLayout("ATC"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _atc6EnabledLamp.On.Bind(ViewModel.IsAtc6EnabledLit);
            row1Children.Add(_atc6EnabledLamp);
            _atcTurnOffLamp = new Lamp(context, CreateTextLayout("切"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _atcTurnOffLamp.On.Bind(ViewModel.IsAtcTurnOffLit);
            row1Children.Add(_atcTurnOffLamp);
            if (showAtsSLamps)
            {
                _atsSPowerLamp = new Lamp(context, CreateTextLayout("ATS 電源"),
                    offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Yellow,
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(),
                    onBorderRadius: lampOnBorderRadius,
                    onInnerShadowWidth: lampOnInnerShadowWidth,
                    expandToFillGaps: true);
                _atsSPowerLamp.On.Bind(ViewModel.IsAtsSPowerLit);
                row1Children.Add(_atsSPowerLamp);
            }

            _patternClearedLamp = new Lamp(context, CreateTextLayout("パターン低滅"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _patternClearedLamp.On.Bind(ViewModel.IsPatternClearedLit);
            row1Children.Add(_patternClearedLamp);
            _emergencyRunLamp = new Lamp(context, CreateTextLayout("非常運転"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _emergencyRunLamp.On.Bind(ViewModel.IsEmergencyRunLit);
            row1Children.Add(_emergencyRunLamp);
            var row2Children = new List<Widget>
                { inchingActivatedLampProperties.IsBottom ? _inchingActivatedLamp : null };
            AtcServiceBrakeLamp = new Lamp(context, CreateTextLayout("ATC 常用"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            AtcServiceBrakeLamp.On.Bind(ViewModel.IsAtcServiceBrakeLit);
            row2Children.Add(AtcServiceBrakeLamp);
            _atcEmergencyBrakeLamp = new Lamp(context, CreateTextLayout("ATC 非常"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _atcEmergencyBrakeLamp.On.Bind(ViewModel.IsAtcEmergencyBrakeLit);
            row2Children.Add(_atcEmergencyBrakeLamp);
            _overrunActionLamp = new Lamp(context, CreateTextLayout("停通防止動作"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _overrunActionLamp.On.Bind(ViewModel.IsOverrunActionLit);
            row2Children.Add(_overrunActionLamp);
            if (showAtsSLamps)
            {
                _atsSActivatedLamp = new Lamp(context, CreateTextLayout("ATS 動作"),
                    offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                    onBackgroundColor: MonitorColors.Red,
                    preferredWidth: LayoutLength.Flex(),
                    preferredHeight: LayoutLength.Flex(),
                    onBorderRadius: lampOnBorderRadius,
                    onInnerShadowWidth: lampOnInnerShadowWidth,
                    expandToFillGaps: true);
                _atsSActivatedLamp.On.Bind(ViewModel.IsAtsSActivatedLit);
                row2Children.Add(_atsSActivatedLamp);
            }

            _atcPowerLamp = new Lamp(context, CreateTextLayout("ATC 電源"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _atcPowerLamp.On.Bind(ViewModel.IsAtcPowerLit);
            row2Children.Add(_atcPowerLamp);
            _atcCutoutLamp = new Lamp(context, CreateTextLayout("ATC 開放"),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth,
                expandToFillGaps: true);
            _atcCutoutLamp.On.Bind(ViewModel.IsAtcCutoutLit);
            row2Children.Add(_atcCutoutLamp);
            var grid = new RowGrid(context, bounds, rowSpacing, lampSpacing, new ICollection<Widget>[]
            {
                row1Children, row2Children
            }, positionSnapToPixels: true);
            AddChild(grid);
            _lampAdjacencyManager.AddFromRowGrid(grid, rowSpacing);
        }

        public Lamp AtcServiceBrakeLamp { get; }
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private TextLayout CreateTextLayout(string text, float sizeLimit = 0, float? fontSize = null)
        {
            return new TextLayout(Context,
                Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, fontSize ?? _fontSize,
                    fontWeight: FontWeight.DemiBold),
                text, ContentOrientation.Vertical,
                arrangement: text.Contains("\n") ? ContentArrangement.Step : ContentArrangement.Center,
                sizeLimit: sizeLimit);
        }

        public struct InchingActivatedLampProperties
        {
            public readonly bool IsBottom;
            public readonly float SizeLimit;
            public readonly string Text;
            public readonly float FontSize;
            public readonly IValueSignal<bool> IsVisible;

            public InchingActivatedLampProperties(bool isBottom, float sizeLimit, string text, float fontSize,
                IValueSignal<bool> isVisible)
            {
                IsBottom = isBottom;
                SizeLimit = sizeLimit;
                Text = text;
                FontSize = fontSize;
                IsVisible = isVisible;
            }
        }
    }
}