using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.D01AX.EDen;
using JREMonitors.E233.TIMS.D01AX.MDen;
using JREMonitors.E233.TIMS.ICCard;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXBaseInfoGroup : Widget<D01AXBaseInfoGroupViewModel>
    {
        public const float FirstRowY = 66;
        public const float SecondRowY = FirstRowY + 22;

        public D01AXBaseInfoGroup(RenderContext context, ScopedRenderContext scopedContext) : base(context)
        {
            ViewModel = new D01AXBaseInfoGroupViewModel();
            var firstRowTrainNumber = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(ViewModel.FirstRowTrainNumber)),
                    horizontalAlignment: 1, context: scopedContext),
                contentColor: "#6BACD5".ToColor4(),
                x: 640,
                y: FirstRowY
            );
            AddChild(firstRowTrainNumber);
            var trainNumberChar = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.TrainNumberCharText)),
                    horizontalAlignment: 1, context: scopedContext),
                contentColor: MonitorColors.TIMSTitleGrey,
                x: 695,
                y: FirstRowY
            );
            AddChild(trainNumberChar);
            var trainTypeSettingCompletedText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("設定完了", horizontalAlignment: 0.5f, verticalAlignment: 0.5f),
                targetBounds: new RectangleF(705, FirstRowY - 1, 90, 20),
                backgroundColor: Colors.Aqua,
                contentColor: MonitorColors.TIMSScreenBackground);
            trainTypeSettingCompletedText.IsVisible.Bind(ViewModel.IsTrainTypeSettingCompletedVisible);
            AddChild(trainTypeSettingCompletedText);
            var secondRowTrainNumber = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    new[]
                    {
                        new BitmapScaleDrawer.DrawerProperties(this.CreateTIMSTextLayout("列車番号")),
                        new BitmapScaleDrawer.DrawerProperties(
                            this.CreateTIMSTextLayout(documentSource: CreateComputed(() =>
                                RichTextParser.Raw(ViewModel.SecondRowTrainNumber)), context: scopedContext), 2,
                            color: MonitorColors.TIMSTitleGreen),
                    }, spacing: 8, context: scopedContext),
                contentColor: MonitorColors.TIMSTitleGrey, y: SecondRowY);
            secondRowTrainNumber.X.Bind(CreateComputed<float>(() =>
                ViewModel.DisplayMode == TIMSDisplayMode.MDen ? 99 : 34));
            AddChild(secondRowTrainNumber);
            var radioChannel = new TIMSRadioChannelWidget(context, scopedContext, SecondRowY);
            radioChannel.RadioChannel.Bind(ViewModel.RadioChannel);
            AddChild(radioChannel);
            var trainTypeText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.TrainType)),
                    horizontalAlignment: 0.5f, verticalAlignment: 0.5f,
                    useHorizontalOverhangMetrics: true),
                targetBounds: new RectangleF(705, 0, 90, 20), backgroundColor: Colors.Yellow,
                contentColor: MonitorColors.TIMSScreenBackground);
            trainTypeText.IsVisible.Bind(ViewModel.IsTrainTypeVisible);
            trainTypeText.Y.Bind(CreateComputed(() =>
                ViewModel.DisplayMode == TIMSDisplayMode.MDen ? FirstRowY + 20 - 1 : FirstRowY + 40 - 1));
            AddChild(trainTypeText);
            var mDenHeader = new D01AXMDenHeader(context);
            mDenHeader.IsVisible.Bind(CreateComputed(() => ViewModel.DisplayMode == TIMSDisplayMode.MDen));
            AddChild(mDenHeader);
            var eDenHeader = new D01AXEDenHeader(context, scopedContext);
            eDenHeader.IsVisible.Bind(CreateComputed(() => ViewModel.DisplayMode == TIMSDisplayMode.EDen));
            eDenHeader.DutyNumber.Bind(ViewModel.DutyNumber);
            eDenHeader.StandardOperatingSpeed.Bind(ViewModel.StandardOperatingSpeed);
            AddChild(eDenHeader);
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }

    public class D01AXBaseInfoGroupViewModel : TIMSViewModel
    {
        private readonly Signal<string> _firstRowTrainNumberRaw = new Signal<string>();
        private readonly Signal<string> _secondRowTrainNumberRaw = new Signal<string>();

        public D01AXBaseInfoGroupViewModel()
        {
            FirstRowTrainNumber = CreateComputed(() =>
                {
                    if (string.IsNullOrEmpty(_firstRowTrainNumberRaw))
                        return string.Empty;
                    var number = TIMSHelper.FormatTrainNumber(_firstRowTrainNumberRaw, '0', prefixPadCount: 1);
                    if (PassSetting)
                    {
                        number = "通" + number;
                    }

                    return number.ToFullWidth();
                }
            );
            SecondRowTrainNumber = CreateComputed(() =>
                string.IsNullOrEmpty(_secondRowTrainNumberRaw)
                    ? string.Empty
                    : TIMSHelper.FormatTrainNumber(_secondRowTrainNumberRaw, null).PadLeft(8).ToFullWidth());
        }

        public Signal<TIMSDisplayMode> DisplayMode { get; } = new Signal<TIMSDisplayMode>();
        public Signal<string> DutyNumber { get; } = new Signal<string>(new string('\u3000', 8));
        public Signal<string> StandardOperatingSpeed { get; } = new Signal<string>(new string('\u3000', 4));
        public Signal<string> RadioChannel { get; } = new Signal<string>(new string('\u3000', 2));
        public Signal<string> TrainNumberCharText { get; } = new Signal<string>("\u3000列番");
        public Computed<string> FirstRowTrainNumber { get; }
        public Computed<string> SecondRowTrainNumber { get; }
        public Signal<string> TrainType { get; } = new Signal<string>(new string('\u3000', 4));
        public Signal<bool> PassSetting { get; } = new Signal<bool>();
        public Signal<bool> IsTrainTypeVisible { get; } = new Signal<bool>();
        public Signal<bool> IsTrainTypeSettingCompletedVisible { get; } = new Signal<bool>();

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            PassSetting.Value = TIMSService.PassSetting;
            DutyNumber.Value = ICCardService.DutyNumber?.ToFullWidth();
            StandardOperatingSpeed.Value = ICCardService.CurrentStandardOperatingSpeed;
            var rawTrainNumber = ICCardService.CurrentTrainNumber;
            if (string.IsNullOrEmpty(rawTrainNumber) &&
                (TIMSService.HasPassSetting || TIMSService.HasSettingCompleted))
                _firstRowTrainNumberRaw.Value = "0000";
            else if (!string.IsNullOrEmpty(rawTrainNumber))
                _firstRowTrainNumberRaw.Value = rawTrainNumber.Trim();
            else _firstRowTrainNumberRaw.Value = string.Empty;
            var trainNumberChar = ICCardService.CurrentLeg?.TrainNumberChar;
            RadioChannel.Value = TIMSHelper.FormatRadioChannel(ICCardService.CurrentRadioChannel);
            _secondRowTrainNumberRaw.Value = (rawTrainNumber ?? string.Empty).Trim();
            DisplayMode.Value = ICCardService.CurrentDisplayMode;
            TrainNumberCharText.Value = (trainNumberChar?.ToString().ToFullWidth() ?? "\u3000") + "列番";
            TrainType.Value = TIMSService.PassSetting ? "通過設定" :
                ICCardService.CurrentTrainType == TIMSTrainType.Local ? "各停" : "快速";
            IsTrainTypeVisible.Value = ICCardService.CurrentTrainType != null || TIMSService.PassSetting;
            IsTrainTypeSettingCompletedVisible.Value = TIMSService.IsSettingCompleted;
        }
    }
}