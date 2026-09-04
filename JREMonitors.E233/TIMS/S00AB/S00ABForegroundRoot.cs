using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Svg;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.X00AA;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.S00AB
{
    public class S00ABForegroundRoot : Widget<S00ABForegroundRootViewModel>
    {
        private const float ButtonHeight = 250f;
        private const float ChangeToTIMSMainButtonWidth = 150f;
        private const float ChangeToTidButtonWidth = 180f;
        private readonly TIMSButton _changeToTidButton;
        private readonly TIMSButton _changeToTIMSMainButton;
        private readonly BoundsDrawerWidget _changeToTIMSMainPrompt;
        private readonly TIMSButton _runStateButton;

        public S00ABForegroundRoot(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            ViewModel = new S00ABForegroundRootViewModel(spec);
            AddChild(new TIMSScreenTitle(context, ScreenIds.S00AB, "初期選択"));
            var homeButton = new TIMSButton(context, new Vector2(X00AABackgroundRoot.Padding),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Width),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Height),
                Context.TIMS().HomeButtonStyle,
                new SvgBoundsDrawer(context, new SvgDocumentProperties(Icons.Home)), reboundImmediate: true
            );
            homeButton.OnClick += () => RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen,
                ViewModel.MonitorType == E233MonitorType.TIMSMain ? 0.2f : 1,
                () =>
                {
                    string targetId;
                    switch (ViewModel.MonitorType.Value)
                    {
                        case E233MonitorType.Meter:
                            targetId = ScreenIds.Meter;
                            break;
                        case E233MonitorType.TIMSMain:
                            targetId = ScreenIds.X00AA;
                            break;
                        case E233MonitorType.Tid:
                        default:
                            targetId = ScreenIds.Tid;
                            break;
                    }

                    Context.DisplayController.RequestChangeScreen(targetId);
                });
            AddChild(homeButton);
            _changeToTIMSMainButton = new TIMSButton(context,
                new Vector2(400 - ChangeToTIMSMainButtonWidth / 2, X00AABackgroundRoot.Padding),
                LayoutLength.Absolute(ChangeToTIMSMainButtonWidth),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Height), context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("メイン画面切替", 1, 1, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true);
            _changeToTIMSMainButton.IsVisible.Bind(ViewModel.IsNotTIMSMain);
            _changeToTIMSMainButton.OnClick +=
                () => RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                    () => { ViewModel.ChangeToTIMSMain(); });
            AddChild(_changeToTIMSMainButton);
            _changeToTidButton = new TIMSButton(context,
                new Vector2(400 - ChangeToTidButtonWidth / 2, X00AABackgroundRoot.Padding),
                LayoutLength.Absolute(ChangeToTidButtonWidth),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Height), context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("保安表示灯画面切替", 1, 1, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true);
            _changeToTidButton.IsVisible.Bind(CreateComputed(() =>
                ViewModel.IsTIMSMain.Value && !ViewModel.HasOtherMonitorToShowSafetyLamps.Value));
            _changeToTidButton.OnClick += () => RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen,
                1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.Tid); });
            AddChild(_changeToTidButton);
            var driverButton = new TIMSButton(context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("運転士", 2, 2, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true
            );
            driverButton.OnClick += () => RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D00AA); });
            var conductorButton = new TIMSButton(context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("車\u3000掌", 2, 2, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true
            );
            conductorButton.OnClick += () => RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.C00AA); });
            var runStateDrawerProperties = new BitmapScaleDrawer.DrawerProperties(this.CreateTIMSTextLayout(
                documentSource: CreateComputed(() => RichTextParser.Raw(ViewModel.IsTIMSMain.Value ? "運転状況" : "")),
                useVerticalOverhangMetrics: true), scaleY: 2);
            runStateDrawerProperties.ScaleX.Bind(CreateComputed<float>(() => ViewModel.SupportsSuica ? 1 : 2));
            _runStateButton = new TIMSButton(context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer(new[] { runStateDrawerProperties }, horizontalAlignment: 0.5f,
                    verticalAlignment: 0.5f)
            );
            _runStateButton.Clickable.Bind(ViewModel.IsTIMSMain);
            var testRunButton = new TIMSButton(context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer("試運転", 2, 2, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true
            );
            var buttons = new List<Widget> { driverButton, conductorButton, _runStateButton, testRunButton };
            var suicaButton = new TIMSButton(context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                context.TIMS().TextButtonStyle,
                this.CreateTIMSTextDrawer(
                    documentSource: CreateComputed(() => RichTextParser.Raw(ViewModel.IsTIMSMain.Value ? "Ｓｕｉｃａ" : "")),
                    1, 2, 0.5f, 0.5f, useVerticalOverhangMetrics: true),
                reboundImmediate: true
            );
            suicaButton.Clickable.Bind(ViewModel.IsTIMSMain);
            suicaButton.IncludeInTotalMajorDimensionSizeWhenHidden.Bind(ViewModel.SupportsSuica);
            var hasGreenCar = CreateComputed(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                return formationSpec != null && formationSpec.HasGreenCar;
            });
            suicaButton.IsVisible.Bind(hasGreenCar);
            buttons.Add(suicaButton);
            var row = new Row(context, 400, 300, fallbackFlexUnitHeight: ButtonHeight, widgets: buttons,
                rowHorizontalAlignment: 0.5f, rowVerticalAlignment: 0.5f, positionSnapToPixels: true);
            row.FallbackFlexUnitWidth.Bind(CreateComputed<float>(() => ViewModel.SupportsSuica ? 140 : 175));
            AddChild(row);
            _changeToTIMSMainPrompt = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("※\u3000メイン画面切替キ一タッチにより、メインのTIMS画面を切替えます。", 1, 1, 0.5f, 0.5f,
                    useVerticalOverhangMetrics: true),
                contentColor: "#6CCDC4".ToColor4(),
                x: 400,
                y: MathHelper.Lerp(300 + ButtonHeight / 2, 599, 0.5f));
            _changeToTIMSMainPrompt.IsVisible.Bind(CreateComputed(() => !ViewModel.IsTIMSMain.Value));
            AddChild(_changeToTIMSMainPrompt);
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, hasGreenCar, ViewModel.SupportsSuica);
        }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);


        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class S00ABForegroundRootViewModel : TIMSFormationViewModel
    {
        private E233MonitorStates _monitorStates;

        public S00ABForegroundRootViewModel(TIMSVehicleSpec spec) : base(spec)
        {
            IsTIMSMain = CreateComputed(() => MonitorType == E233MonitorType.TIMSMain);
            IsNotTIMSMain = CreateComputed(() => MonitorType != E233MonitorType.TIMSMain);
            SupportsSuica = CreateComputed(() =>
            {
                var formationSpec = FormationSpec.Value;
                return formationSpec != null && formationSpec.SupportsSuica;
            });
        }

        public Signal<bool> HasOtherMonitorToShowSafetyLamps { get; } = new Signal<bool>();
        public Signal<E233MonitorType> MonitorType { get; } = new Signal<E233MonitorType>();
        public Computed<bool> IsTIMSMain { get; }
        public Computed<bool> IsNotTIMSMain { get; }
        public Computed<bool> SupportsSuica { get; }

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            MonitorType.Value = _monitorStates.MonitorType;
            HasOtherMonitorToShowSafetyLamps.Value = _monitorStates.HasOtherMonitorToShowSafetyLamps;
        }

        public void ChangeToTIMSMain()
        {
            _monitorStates.RequestChangeToTIMSMain();
        }
    }
}