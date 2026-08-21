using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.D01AX;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AXBaseInfoGroup : Widget<C01AXBaseInfoGroupViewModel>
    {
        public C01AXBaseInfoGroup(RenderContext context) : base(context)
        {
            ViewModel = new C01AXBaseInfoGroupViewModel();
            var radioChannelWidget = new TIMSRadioChannelWidget(context, y: D01AXBaseInfoGroup.FirstRowY);
            radioChannelWidget.RadioChannel.Bind(ViewModel.RadioChannel);
            AddChild(radioChannelWidget);
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("外気温"),
                contentColor: MonitorColors.TIMSTitleGrey, x: 640, y: D01AXBaseInfoGroup.FirstRowY));
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => new RichTextBuilder()
                        .Append(ViewModel.ExternalTemperature.Value.ToString("F1").ToFullWidth(),
                            color: MonitorColors.White)
                        .Append("℃")
                        .Build()),
                    horizontalAlignment: 1, cache: false), contentColor: MonitorColors.TIMSTitleGrey, x: 795,
                y: D01AXBaseInfoGroup.FirstRowY));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
    }

    public class C01AXBaseInfoGroupViewModel : TIMSViewModel
    {
        public Signal<string> RadioChannel { get; } = new Signal<string>();
        public Signal<float> ExternalTemperature { get; } = new Signal<float>();

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            RadioChannel.Value = TIMSHelper.FormatRadioChannel(ICCardService.CurrentRadioChannel);
            ExternalTemperature.Value = TIMSService.ExternalTemperature;
        }
    }
}