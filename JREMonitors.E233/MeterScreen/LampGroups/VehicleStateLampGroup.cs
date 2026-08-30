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
    public class VehicleStateLampGroup : Widget<VehicleStateLampGroupViewModel>
    {
        private readonly Lamp _accLamp;
        private readonly Lamp _constantSpeedLamp;
        private readonly Lamp _directAirBackupBrakeLamp;
        private readonly Lamp _emergencyShuntLamp;
        private readonly Lamp _snowBrakeLamp;
        private readonly Lamp _springBrakeLamp;
        private readonly Lamp _threePhaseLamp;

        public VehicleStateLampGroup(
            RenderContext context,
            RectangleF bounds,
            float lampSpacing,
            float onStaticExtensionHeight,
            float firstLineFontSize,
            float secondLineFontSize,
            bool singleLineOnly,
            float snowBrakeLampSizeLimit,
            LayoutLength springBrakeLampPreferredWidth,
            LayoutLength springBrakeLampPreferredHeight,
            float lampOnBorderRadius,
            float? springBrakeFontSize = null,
            float lampOnInnerShadowWidth = 3
        ) : base(context)
        {
            ViewModel = new VehicleStateLampGroupViewModel();
            _accLamp = new Lamp(context, preferredWidth: LayoutLength.Flex(), preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _accLamp.On.Bind(ViewModel.IsAccLit);
            _threePhaseLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    "三\u2002相", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _threePhaseLamp.On.Bind(ViewModel.IsThreePhaseLit);
            _emergencyShuntLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    "非常短絡", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _emergencyShuntLamp.On.Bind(ViewModel.IsEmergencyShuntLit);
            TextLayout snowBrakeTextLayout;
            if (singleLineOnly)
                snowBrakeTextLayout = new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    "耐雪ブレーキ", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true);
            else
                snowBrakeTextLayout = new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    new RichTextBuilder().Append("耐雪\n").Append("ブレーキ", secondLineFontSize).Build(),
                    ContentOrientation.Vertical, arrangement: ContentArrangement.Step,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true,
                    sizeLimit: snowBrakeLampSizeLimit,
                    fixedLineSpacing: -22,
                    finalOffsetCrossAxis: 1);
            _snowBrakeLamp = new Lamp(context, snowBrakeTextLayout,
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _snowBrakeLamp.On.Bind(ViewModel.IsSnowBrakeLit);
            _directAirBackupBrakeLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    "直通予備", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _directAirBackupBrakeLamp.On.Bind(ViewModel.IsDirectAirBackupBrakeLit);
            _constantSpeedLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, firstLineFontSize,
                        fontWeight: FontWeight.DemiBold),
                    "定\u3000速", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true),
                preferredWidth: LayoutLength.Flex(),
                preferredHeight: LayoutLength.Flex(),
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Yellow,
                onStaticExtensionHeight: onStaticExtensionHeight,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _constantSpeedLamp.On.Bind(ViewModel.IsConstantSpeedLit);
            _springBrakeLamp = new Lamp(context, new TextLayout(Context,
                    Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily,
                        springBrakeFontSize ?? firstLineFontSize, fontWeight: FontWeight.DemiBold),
                    "駐車ブレーキ", ContentOrientation.Vertical, arrangement: ContentArrangement.Center,
                    flowDirection: ContentFlowDirection.Reverse, useHorizontalOverhangMetrics: true),
                preferredWidth: springBrakeLampPreferredWidth,
                preferredHeight: springBrakeLampPreferredHeight,
                offBackgroundColor: MonitorColors.PanelLampOffBackgroundColor,
                onBackgroundColor: MonitorColors.Red,
                offOutlineWidth: 5,
                onBorderRadius: lampOnBorderRadius,
                onInnerShadowWidth: lampOnInnerShadowWidth);
            _springBrakeLamp.On.Bind(ViewModel.IsSpringBrakeLit);
            var row = Row.FromBounds(context, bounds, lampSpacing,
                new Widget[]
                {
                    _accLamp, _threePhaseLamp, _emergencyShuntLamp, _snowBrakeLamp, _directAirBackupBrakeLamp,
                    _constantSpeedLamp, _springBrakeLamp
                }, widgetVerticalAlign: 0.5f, positionSnapToPixels: true);
            AddChild(row);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}