using System.Collections.Generic;
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
        private const string IdSafetyLampsHidden = "SafetyLampsHidden";
        private const string IdVisibleWithTasc = "VisibleWithTasc";
        private const string IdVisibleWithoutTasc = "VisibleWithoutTasc";

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
        private readonly ShadowedMeterNum _deviceVoltNum;
        private readonly Lamp _ebLamp;
        private readonly Lamp _holdSpeedLamp;
        private readonly WidgetSwitcher _lampPanelSwitcher;
        private readonly LampPanel _lampPanelWhenSafetyLampsHidden;
        private readonly MrForeground _mrForeground;
        private readonly List<LampPanel> _safetyLampsVisiblePanels = new List<LampPanel>();
        private readonly bool _showHoldSpeedLamp;
        private readonly MeterNum _speedNum;
        protected readonly InfoButtonGroup InfoButtonGroup;
        protected readonly float SpeedOffsetY;
        protected RootProperties RootPropertiesWithoutSafetyLamps;
        protected RootProperties RootPropertiesWithSafetyLamps;

        public MeterForegroundRootBase(
            RenderContext context,
            RootProperties propertiesWithSafetyLamps,
            RootProperties propertiesWithoutSafetyLamps,
            float speedOffsetY
        ) : base(context)
        {
            ViewModel = new MeterForegroundRootBaseViewModel();
            RootPropertiesWithSafetyLamps = propertiesWithSafetyLamps;
            RootPropertiesWithoutSafetyLamps = propertiesWithoutSafetyLamps;
            SpeedOffsetY = speedOffsetY;
            _showHoldSpeedLamp = propertiesWithSafetyLamps.ShowHoldSpeedLamp;
            InfoButtonGroup = new InfoButtonGroup(context, 10, new Vector2(1024, 768));
            AddChild(InfoButtonGroup);
            _speedNum = new MeterSpeedNum(context, 840, 728 + SpeedOffsetY);
            AddChild(_speedNum);
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

            _deviceVoltGaugeNeedle = new GaugeNeedle(context, 0, 0,
                DeviceVoltGaugeBackground.SectorRadius + DeviceVoltGaugeBackground.TickMarkSpacing + 2,
                Shadows.GaugeNeedleInnerSmall);
            AddChild(_deviceVoltGaugeNeedle);
            _deviceVoltNum = new ShadowedMeterNum(context, 0, 0, DeviceVoltGaugeBackground.TickTextSize + 2,
                Shadows.VoltNumDrop, 78, 53);
            _deviceVoltNum.IsVisible.Bind(CreateComputed(() => ViewModel.DeviceVoltage <= 85));
            _deviceVoltNum.Num.Bind(ViewModel.DeviceVoltage);
            AddChild(_deviceVoltNum);
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
                var activeLampPanelId = ResolveActiveLampPanelId();
                _lampPanelWhenSafetyLampsHidden.Clickable = ViewModel.IsSafetyLampsVisibleExternally;
                foreach (var panel in _safetyLampsVisiblePanels)
                    panel.Clickable = ViewModel.IsSafetyLampsVisibleExternally;
                OnLampPanelStateResolved(activeLampPanelId);
                _lampPanelSwitcher.SetActiveWidget(activeLampPanelId);
                _deviceVoltGaugeNeedle.Degree.Value = -210 + 210 * ViewModel.DeviceVoltage / 150;
                _catenaryVoltGaugeNeedle.Degree.Value = -210 + 210 * ViewModel.CatenaryVoltage / 2000;
                _speedNum.Num.Value = ViewModel.Speed;
                _bcForeground.Bc.Value = ViewModel.BcPressure;
                _bcForeground.Highlight200Kpa.Value = ViewModel.Highlight200Kpa;
                _mrForeground.Mr.Value = ViewModel.MrPressure;
                _brakeForeground.Brake.Value = ViewModel.Brake;
                _ebLamp.On.Value = ViewModel.Eb;
                if (_showHoldSpeedLamp) _holdSpeedLamp.On.Value = ViewModel.HoldSpeed;
            });
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                context.DisplayController.RequestReset();
            }, ViewModel.SupportsTasc);
        }

        protected void ApplyLayout(RootProperties properties)
        {
            _deviceVoltGaugeNeedle.X.Value = properties.DeviceVoltageGaugePos.X;
            _deviceVoltGaugeNeedle.Y.Value = properties.DeviceVoltageGaugePos.Y;
            _deviceVoltGaugeNeedle.Scale.Value = properties.DeviceVoltageSectorRadius /
                                                 DeviceVoltGaugeBackground.SectorRadius;
            _deviceVoltNum.X.Value = properties.DeviceVoltageGaugePos.X;
            _deviceVoltNum.Y.Value = properties.DeviceVoltageGaugePos.Y;
            _deviceVoltNum.Scale.Value = properties.DeviceVoltageSectorRadius / DeviceVoltGaugeBackground.SectorRadius;
            _catenaryVoltGaugeNeedle.X.Value = properties.CatenaryVoltageGaugePos.X;
            _catenaryVoltGaugeNeedle.Y.Value = properties.CatenaryVoltageGaugePos.Y;
            _catenaryVoltGaugeNeedle.Scale.Value =
                properties.CatenaryVoltageSectorRadius / CatenaryVoltGaugeBackground.SectorRadius;
            _ebLamp.Y.Value = properties.ShowHoldSpeedLamp ? 290 : 333;
        }

        protected virtual void OnSafetyLampsVisible()
        {
            ApplyLayout(RootPropertiesWithSafetyLamps);
        }

        protected virtual string ResolveActiveLampPanelId()
        {
            if (!ViewModel.IsSafetyLampVisible) return IdSafetyLampsHidden;
            return ViewModel.SupportsTasc ? IdVisibleWithTasc : IdVisibleWithoutTasc;
        }

        protected virtual void OnLampPanelStateResolved(string activeLampPanelId)
        {
            if (activeLampPanelId == IdSafetyLampsHidden) ApplyLayout(RootPropertiesWithoutSafetyLamps);
            else OnSafetyLampsVisible();
        }

        protected void AddLampPanel(string id, LampPanel lampPanel)
        {
            RegisterSafetyLampsVisiblePanel(lampPanel, id);
        }

        protected void AddLampPanelWhenSafetyLampsVisible(LampPanel lampPanel)
        {
            AddLampPanel(IdVisibleWithTasc, lampPanel);
        }

        protected void AddLampPanelWhenSafetyLampsVisibleWithoutTasc(LampPanel lampPanel)
        {
            AddLampPanel(IdVisibleWithoutTasc, lampPanel);
        }

        protected static LampPanel CreateSafetyLampsVisiblePanel(RenderContext context, params Widget[] lamps)
        {
            return new LampPanel(context, 1023, 432, 310, children: lamps);
        }

        private void RegisterSafetyLampsVisiblePanel(LampPanel lampPanel, string id)
        {
            if (_lampPanelSwitcher.Add(id, lampPanel))
            {
                lampPanel.OnClick += OnLampPanelClick;
                _safetyLampsVisiblePanels.Add(lampPanel);
            }
        }

        private LampPanel CreateLampPanelWhenSafetyLampsHidden()
        {
            var vehicleStateLampGroupWhenSafetyLampsHidden = new VehicleStateLampGroup(Context,
                new RawRectF(
                    608,
                    11,
                    1010,
                    120
                ),
                10,
                44,
                24,
                20,
                false,
                90,
                LayoutLength.Flex(),
                LayoutLength.Absolute(153),
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