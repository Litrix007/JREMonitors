using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.MeterScreen.Base;
using JREMonitors.E233.MeterScreen.Foreground.Base;
using JREMonitors.E233.MeterScreen.LampGroups;
using JREMonitors.E233.Needles;
using JREMonitors.E233.ViewModels;
using Vortice;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground
{
    public abstract class MeterForegroundRootBase : Widget<MeterForegroundRootBaseViewModel>
    {
        private static readonly string IdSafetyLampsHidden = "SafetyLampsHidden";
        private static readonly string IdSafetyLampsVisible = "SafetyLampsVisible";

        private static readonly DropShadow[] EbLampOnDropShadows =
        {
            new DropShadow
            {
                OffsetX = 4,
                Color = new Color4(0, 0, 0, 100)
            }
        };

        private readonly BcForeground _bcForeground;
        private readonly BrakeForeground _brakeForeground;
        private readonly GaugeNeedle _catenaryVoltGaugeNeedle;
        private readonly GaugeNeedle _deviceVoltGaugeNeedle;
        private readonly Lamp _ebLamp;
        private readonly Lamp _holdSpeedLamp;
        private readonly WidgetSwitcher _lampPanelSwitcher;
        private readonly LampPanel _lampPanelWhenSafetyLampsHidden;
        private readonly MrForeground _mrForeground;
        private readonly bool _showHoldSpeedLamp;
        private readonly SpeedNumTitle _speedNumTitle;
        protected readonly InfoButtonGroup InfoButtonGroup;
        private LampPanel _lampPanelWhenSafetyLampsVisible;

        protected RootProperties RootPropertiesWithoutSafetyLamps;
        protected RootProperties RootPropertiesWithSafetyLamps;

        public MeterForegroundRootBase(
            RenderContext context,
            RootProperties propertiesWithSafetyLamps,
            RootProperties propertiesWithoutSafetyLamps
        ) : base(context)
        {
            ViewModel = new MeterForegroundRootBaseViewModel();
            RootPropertiesWithSafetyLamps = propertiesWithSafetyLamps;
            RootPropertiesWithoutSafetyLamps = propertiesWithoutSafetyLamps;
            _showHoldSpeedLamp = propertiesWithSafetyLamps.ShowHoldSpeedLamp;
            InfoButtonGroup = new InfoButtonGroup(context, 10, new Vector2(1024, 768));
            AddChild(InfoButtonGroup);
            _speedNumTitle = new SpeedNumTitle(context, 840, 728 + propertiesWithSafetyLamps.SpeedOffsetY);
            AddChild(_speedNumTitle);
            _mrForeground = new MrForeground(context, 354, 333);
            AddChild(_mrForeground);
            _brakeForeground = new BrakeForeground(context, 128, 730);
            AddChild(_brakeForeground);
            _ebLamp = new Lamp(context,
                new TextLayout(context,
                    context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 22, fontWeight: FontWeight.Bold),
                    "非常"),
                128 - BrakeBackground.LongWidth, _showHoldSpeedLamp ? 290 : 333,
                LayoutLength.Absolute(BrakeBackground.LongWidth),
                LayoutLength.Absolute(32), offBackgroundColor: Colors.Transparent,
                offBorderColor: LampBorderColor.EbRedOff,
                offBorderWidth: 3, offBorderRadius: 5, onStaticExtensionHeight: 16,
                onBackgroundColor: MonitorColors.Red,
                onOutlineWidth: 3, onOutlineRadius: 2, onOutlineColor: LampBorderColor.EbRedOn,
                onInnerShadowAlphaRatio: 0.75f,
                onDropShadows: EbLampOnDropShadows,
                hiddenWhenOff: true,
                truncateBottomRightInnerShadow: false);
            AddChild(_ebLamp);
            if (_showHoldSpeedLamp)
            {
                _holdSpeedLamp = new Lamp(context,
                    new TextLayout(context,
                        context.FontManager.GetOrCreateFormat(Fonts.YuGothicUiFamily, 22, fontWeight: FontWeight.Bold),
                        "抑速"),
                    128 - BrakeBackground.LongWidth, 333, LayoutLength.Absolute(BrakeBackground.LongWidth),
                    LayoutLength.Absolute(32), offBackgroundColor: new Color4(0, 0, 0, 0),
                    offBorderColor: LampBorderColor.HoldSpeedGreen,
                    offBorderWidth: 3, offBorderRadius: 5, onBackgroundColor: MonitorColors.HoldSpeedGreen,
                    onInnerShadowAlphaRatio: 0.75f, hiddenWhenOff: true, truncateBottomRightInnerShadow: false);
                AddChild(_holdSpeedLamp);
            }

            _deviceVoltGaugeNeedle =
                new GaugeNeedle(context, 0, 0,
                    DeviceVoltGaugeBackground.SectorRadius + DeviceVoltGaugeBackground.TickMarkSpacing + 2,
                    Shadows.GaugeNeedleInnerSmall);
            AddChild(_deviceVoltGaugeNeedle);
            if (propertiesWithSafetyLamps.CurrentSectorRadius > 0)
            {
                // TODO 添加电流表指针
            }

            _catenaryVoltGaugeNeedle =
                new GaugeNeedle(context, 0, 0,
                    DeviceVoltGaugeBackground.SectorRadius + DeviceVoltGaugeBackground.TickMarkSpacing + 2,
                    Shadows.GaugeNeedleInnerSmall);
            AddChild(_catenaryVoltGaugeNeedle);
            _bcForeground = new BcForeground(context, 147, 333);
            AddChild(_bcForeground);
            _lampPanelSwitcher = new WidgetSwitcher(context, WidgetSwitcher.RefreshPolicy.FadeOutThenFadeIn);
            _lampPanelWhenSafetyLampsHidden = CreateLampPanelWhenSafetyLampsHidden();
            _lampPanelWhenSafetyLampsHidden.OnClick += OnLampPanelClick;
            _lampPanelSwitcher.Add(IdSafetyLampsHidden, _lampPanelWhenSafetyLampsHidden);
            AddChild(_lampPanelSwitcher);
            WatchEffect(EffectPhase.State, () =>
            {
                _lampPanelWhenSafetyLampsHidden.Clickable = ViewModel.IsSafetyLampsVisibleExternally;
                if (_lampPanelWhenSafetyLampsVisible != null)
                    _lampPanelWhenSafetyLampsVisible.Clickable = ViewModel.IsSafetyLampsVisibleExternally;
                if (ViewModel.IsSafetyLampVisible)
                {
                    _deviceVoltGaugeNeedle.X.Value = RootPropertiesWithSafetyLamps.DeviceVoltageGaugePos.X;
                    _deviceVoltGaugeNeedle.Y.Value = RootPropertiesWithSafetyLamps.DeviceVoltageGaugePos.Y;
                    _deviceVoltGaugeNeedle.Scale.Value = RootPropertiesWithSafetyLamps.DeviceVoltageSectorRadius /
                                                         DeviceVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeNeedle.X.Value = RootPropertiesWithSafetyLamps.CatenaryVoltageGaugePos.X;
                    _catenaryVoltGaugeNeedle.Y.Value = RootPropertiesWithSafetyLamps.CatenaryVoltageGaugePos.Y;
                    _catenaryVoltGaugeNeedle.Scale.Value = RootPropertiesWithSafetyLamps.CatenaryVoltageSectorRadius /
                                                           CatenaryVoltGaugeBackground.SectorRadius;
                }
                else
                {
                    _deviceVoltGaugeNeedle.X.Value = RootPropertiesWithoutSafetyLamps.DeviceVoltageGaugePos.X;
                    _deviceVoltGaugeNeedle.Y.Value = RootPropertiesWithoutSafetyLamps.DeviceVoltageGaugePos.Y;
                    _deviceVoltGaugeNeedle.Scale.Value = RootPropertiesWithoutSafetyLamps.DeviceVoltageSectorRadius /
                                                         DeviceVoltGaugeBackground.SectorRadius;
                    _catenaryVoltGaugeNeedle.X.Value = RootPropertiesWithoutSafetyLamps.CatenaryVoltageGaugePos.X;
                    _catenaryVoltGaugeNeedle.Y.Value = RootPropertiesWithoutSafetyLamps.CatenaryVoltageGaugePos.Y;
                    _catenaryVoltGaugeNeedle.Scale.Value =
                        RootPropertiesWithoutSafetyLamps.CatenaryVoltageSectorRadius /
                        CatenaryVoltGaugeBackground.SectorRadius;
                }

                _lampPanelSwitcher.SetActiveWidget(ViewModel.IsSafetyLampVisible
                    ? IdSafetyLampsVisible
                    : IdSafetyLampsHidden);
                _deviceVoltGaugeNeedle.Degree.Value = -210 + 210 * ViewModel.DeviceVoltage / 150;
                _catenaryVoltGaugeNeedle.Degree.Value = -210 + 210 * ViewModel.CatenaryVoltage / 2000;
                _speedNumTitle.Speed = ViewModel.Speed;
                _bcForeground.Bc.Value = ViewModel.BcPressure;
                _bcForeground.Highlight200Kpa.Value = ViewModel.Highlight200Kpa;
                _mrForeground.Mr.Value = ViewModel.MrPressure;
                _brakeForeground.Brake.Value = ViewModel.Brake;
                _ebLamp.On.Value = ViewModel.Eb;
                if (_showHoldSpeedLamp) _holdSpeedLamp.On.Value = ViewModel.HoldSpeed;
            });
        }

        protected void AddLampPanelWhenSafetyLampsVisible(LampPanel lampPanel)
        {
            _lampPanelWhenSafetyLampsVisible = lampPanel;
            lampPanel.OnClick += OnLampPanelClick;
            _lampPanelSwitcher.Add(IdSafetyLampsVisible, lampPanel);
        }

        private LampPanel CreateLampPanelWhenSafetyLampsHidden()
        {
            var vehicleStateLampGroupWhenSafetyLampsHidden = new VehicleStateLampGroup(Context,
                new RawRectF(
                    608,
                    10,
                    1010,
                    129
                ),
                10,
                44,
                24,
                20,
                false,
                90,
                LayoutLength.Flex(),
                LayoutLength.Absolute(155),
                4
            );
            return new LampPanel(Context, 1023, 432, 185,
                children: new Widget[] { vehicleStateLampGroupWhenSafetyLampsHidden });
        }

        private void OnLampPanelClick()
        {
            ViewModel.ToggleLocalSafetyLampVisibility();
        }
    }
}