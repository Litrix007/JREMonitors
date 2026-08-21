using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.MeterScreen.Base;
using JREMonitors.E233.ViewModels;
using Vortice.DirectWrite;

namespace JREMonitors.E233.MeterScreen.Background
{
    public abstract class MeterBackgroundRootBase : Widget<MeterBackgroundBaseViewModel>
    {
        public static readonly RootProperties CommonRootPropertiesWithoutSafetyLamps = new RootProperties(true,
            new Vector2(155, 165),
            DeviceVoltGaugeBackground.SectorRadius,
            new Vector2(455, 165),
            CatenaryVoltGaugeBackground.SectorRadius,
            Vector2.Zero,
            0,
            0);

        public static readonly RootProperties CommonRootPropertiesWithoutSafetyAndHldLamps = new RootProperties(false,
            new Vector2(155, 165),
            DeviceVoltGaugeBackground.SectorRadius,
            new Vector2(455, 165),
            CatenaryVoltGaugeBackground.SectorRadius,
            Vector2.Zero,
            0,
            0);

        private readonly bool _boldCenterMinorTicksWithoutSafetyLamps;
        private readonly bool _boldCenterMinorTicksWithSafetyLamps;

        private readonly CatenaryVoltGaugeBackground _catenaryVoltGaugeBackground;
        private readonly DeviceVoltGaugeBackground _deviceVoltGaugeBackground;
        private readonly IDWriteTextFormat _format;
        protected RootProperties RootPropertiesWithoutSafetyLamps;
        protected RootProperties RootPropertiesWithSafetyLamps;

        protected MeterBackgroundRootBase(
            RenderContext context,
            RootProperties propertiesWithSafetyLamps,
            bool boldCenterMinorTicksWithSafetyLamps,
            RootProperties propertiesWithoutSafetyLamps,
            bool boldCenterMinorTicksWithoutSafetyLamps
        ) : base(context)
        {
            ViewModel = new MeterBackgroundBaseViewModel();
            RootPropertiesWithSafetyLamps = propertiesWithSafetyLamps;
            _boldCenterMinorTicksWithSafetyLamps = boldCenterMinorTicksWithSafetyLamps;
            RootPropertiesWithoutSafetyLamps = propertiesWithoutSafetyLamps;
            _boldCenterMinorTicksWithoutSafetyLamps = boldCenterMinorTicksWithoutSafetyLamps;
            _format = context.FontManager.GetOrCreateFormat(Fonts.MyriadProFamily, 28);
            AddChild(new BcBackground(context, 147, 333));
            AddChild(new MrBackground(context, 354, 333));
            AddChild(new BrakeBackground(context, 128, 730));
            var ebLamp = new Lamp(context,
                new TextLayout(context,
                    context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 22, fontWeight: FontWeight.Bold),
                    "非常"),
                128 - BrakeBackground.LongWidth, propertiesWithSafetyLamps.ShowHoldSpeedLamp ? 290 : 333,
                LayoutLength.Absolute(BrakeBackground.LongWidth),
                LayoutLength.Absolute(32),
                offBackgroundColor: null, offBorderColor: LampBorderColor.EbRedOff, offBorderWidth: 3,
                offBorderRadius: 4);
            AddChild(ebLamp);
            if (propertiesWithSafetyLamps.ShowHoldSpeedLamp)
            {
                var holdSpeedLamp = new Lamp(context,
                    new TextLayout(context,
                        context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 22, fontWeight: FontWeight.Bold),
                        "抑速"),
                    128 - BrakeBackground.LongWidth, 333, LayoutLength.Absolute(BrakeBackground.LongWidth),
                    LayoutLength.Absolute(32), LampBorderColor.HoldSpeedGreen, 3,
                    4);
                AddChild(holdSpeedLamp);
            }

            _deviceVoltGaugeBackground = new DeviceVoltGaugeBackground(context, 0, 0);
            AddChild(_deviceVoltGaugeBackground);
            if (propertiesWithSafetyLamps.CurrentSectorRadius > 0)
            {
                // TODO 添加电流表背景
            }

            _catenaryVoltGaugeBackground = new CatenaryVoltGaugeBackground(context, 0, 0);
            AddChild(_catenaryVoltGaugeBackground);
            WatchEffect(EffectPhase.State, () =>
            {
                if (ViewModel.IsSafetyLampVisible)
                {
                    _deviceVoltGaugeBackground.X.Value = RootPropertiesWithSafetyLamps.DeviceVoltageGaugePos.X;
                    _deviceVoltGaugeBackground.Y.Value = RootPropertiesWithSafetyLamps.DeviceVoltageGaugePos.Y;
                    _deviceVoltGaugeBackground.Scale.Value = RootPropertiesWithSafetyLamps.DeviceVoltageSectorRadius /
                                                             DeviceVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeBackground.X.Value = RootPropertiesWithSafetyLamps.CatenaryVoltageGaugePos.X;
                    _catenaryVoltGaugeBackground.Y.Value = RootPropertiesWithSafetyLamps.CatenaryVoltageGaugePos.Y;
                    _catenaryVoltGaugeBackground.Scale.Value =
                        RootPropertiesWithSafetyLamps.CatenaryVoltageSectorRadius /
                        CatenaryVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeBackground.BoldCenterMinorTicks = _boldCenterMinorTicksWithSafetyLamps;
                }
                else
                {
                    _deviceVoltGaugeBackground.X.Value = RootPropertiesWithoutSafetyLamps.DeviceVoltageGaugePos.X;
                    _deviceVoltGaugeBackground.Y.Value = RootPropertiesWithoutSafetyLamps.DeviceVoltageGaugePos.Y;
                    _deviceVoltGaugeBackground.Scale.Value =
                        RootPropertiesWithoutSafetyLamps.DeviceVoltageSectorRadius /
                        DeviceVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeBackground.X.Value = RootPropertiesWithoutSafetyLamps.CatenaryVoltageGaugePos.X;
                    _catenaryVoltGaugeBackground.Y.Value = RootPropertiesWithoutSafetyLamps.CatenaryVoltageGaugePos.Y;
                    _catenaryVoltGaugeBackground.Scale.Value =
                        RootPropertiesWithoutSafetyLamps.CatenaryVoltageSectorRadius /
                        CatenaryVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeBackground.BoldCenterMinorTicks = _boldCenterMinorTicksWithoutSafetyLamps;
                }
            });
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected override void OnDraw(float totalScale)
        {
            Context.CommonBrush.Color = MonitorColors.White;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "km/h", 845,
                705 + RootPropertiesWithSafetyLamps.SpeedOffsetY, _format, Context.CommonBrush, angleDegrees: 10);
        }
    }
}