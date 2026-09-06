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
            0);

        public static readonly RootProperties CommonRootPropertiesWithoutSafetyAndHldLamps = new RootProperties(false,
            new Vector2(155, 165),
            DeviceVoltGaugeBackground.SectorRadius,
            new Vector2(455, 165),
            CatenaryVoltGaugeBackground.SectorRadius,
            Vector2.Zero,
            0);

        private readonly CatenaryVoltGaugeBackground _catenaryVoltGaugeBackground;
        private readonly DeviceVoltGaugeBackground _deviceVoltGaugeBackground;
        private readonly Lamp _ebLamp;
        private readonly IDWriteTextFormat _format;
        protected readonly bool ShortenMinorTicksWithoutSafetyLamps;
        protected readonly bool ShortenMinorTicksWithSafetyLamps;
        protected readonly bool BoldCenterMinorTicksWithoutSafetyLamps;
        protected readonly bool BoldCenterMinorTicksWithSafetyLamps;
        protected readonly bool ShowHoldSpeedLamp;
        protected readonly float SpeedOffsetY;
        protected RootProperties RootPropertiesWithoutSafetyLamps;
        protected RootProperties RootPropertiesWithSafetyLamps;

        protected MeterBackgroundRootBase(
            RenderContext context,
            RootProperties propertiesWithSafetyLamps,
            bool shortenMinorTicksWithSafetyLamps,
            bool boldCenterMinorTicksWithSafetyLamps,
            RootProperties propertiesWithoutSafetyLamps,
            bool shortenMinorTicksWithoutSafetyLamps,
            bool boldCenterMinorTicksWithoutSafetyLamps,
            float speedOffsetY
        ) : base(context)
        {
            ViewModel = new MeterBackgroundBaseViewModel();
            RootPropertiesWithSafetyLamps = propertiesWithSafetyLamps;
            ShortenMinorTicksWithSafetyLamps = shortenMinorTicksWithSafetyLamps;
            BoldCenterMinorTicksWithSafetyLamps = boldCenterMinorTicksWithSafetyLamps;
            RootPropertiesWithoutSafetyLamps = propertiesWithoutSafetyLamps;
            BoldCenterMinorTicksWithoutSafetyLamps = boldCenterMinorTicksWithoutSafetyLamps;
            ShortenMinorTicksWithoutSafetyLamps = shortenMinorTicksWithoutSafetyLamps;
            ShowHoldSpeedLamp = propertiesWithSafetyLamps.ShowHoldSpeedLamp;
            SpeedOffsetY = speedOffsetY;
            _format = context.FontManager.GetOrCreateFormat(Fonts.MyriadProFamily, 28);
            AddChild(new BcBackground(context, 147, 333));
            AddChild(new MrBackground(context, 354, 333));
            AddChild(new BrakeBackground(context, 128, 730));
            _ebLamp = new Lamp(context,
                new TextLayout(context,
                    context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 22, fontWeight: FontWeight.Bold),
                    "非常"),
                128 - BrakeBackground.LongWidth, propertiesWithSafetyLamps.ShowHoldSpeedLamp ? 290 : 333,
                LayoutLength.Absolute(BrakeBackground.LongWidth),
                LayoutLength.Absolute(32),
                offBackgroundColor: null, offBorderColor: LampBorderColor.EbRedOff, offBorderWidth: 3,
                offBorderRadius: 4);
            AddChild(_ebLamp);
            if (ShowHoldSpeedLamp)
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
                if (!ViewModel.IsSafetyLampVisible)
                    ApplyLayout(RootPropertiesWithoutSafetyLamps, ShortenMinorTicksWithoutSafetyLamps,
                        BoldCenterMinorTicksWithoutSafetyLamps);
                else
                    OnSafetyLampsVisible();
            });
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected void ApplyLayout(RootProperties properties, bool shortenMinorTicks, bool boldCenterMinorTicks)
        {
            _deviceVoltGaugeBackground.X.Value = properties.DeviceVoltageGaugePos.X;
            _deviceVoltGaugeBackground.Y.Value = properties.DeviceVoltageGaugePos.Y;
            _deviceVoltGaugeBackground.Scale.Value =
                properties.DeviceVoltageSectorRadius / DeviceVoltGaugeBackground.SectorRadius;
            _catenaryVoltGaugeBackground.X.Value = properties.CatenaryVoltageGaugePos.X;
            _catenaryVoltGaugeBackground.Y.Value = properties.CatenaryVoltageGaugePos.Y;
            _catenaryVoltGaugeBackground.Scale.Value =
                properties.CatenaryVoltageSectorRadius / CatenaryVoltGaugeBackground.SectorRadius;
            _catenaryVoltGaugeBackground.SetShortenMinorTickMarks(shortenMinorTicks);
            _catenaryVoltGaugeBackground.BoldCenterMinorTicks = boldCenterMinorTicks;
            _ebLamp.Y.Value = properties.ShowHoldSpeedLamp ? 290 : 333;
        }

        protected virtual void OnSafetyLampsVisible()
        {
            ApplyLayout(RootPropertiesWithSafetyLamps, ShortenMinorTicksWithSafetyLamps,
                BoldCenterMinorTicksWithSafetyLamps);
        }

        protected override void OnDraw(float totalScale)
        {
            Context.CommonBrush.Color = MonitorColors.White;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "km/h", 845,
                705 + SpeedOffsetY, _format, Context.CommonBrush, angleDegrees: 10);
        }
    }
}