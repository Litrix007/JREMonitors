using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenHeader : Widget
    {
        public D01AXEDenHeader(RenderContext context, ScopedRenderContext scopedContext) : base(context)
        {
            DutyNumber = CreateRelayPropertySlot<string>();
            StandardOperatingSpeed = CreateRelayPropertySlot<string>();
            const float y = D01AXBaseInfoGroup.SecondRowY + 22;
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("行路番号"),
                contentColor: MonitorColors.TIMSTitleGrey, x: 34, y: y));
            var dutyNumberText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(DutyNumber)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White, x: 114, y: y);
            dutyNumberText.IsVisible.Bind(CreateComputed(() => !string.IsNullOrEmpty(DutyNumber)));
            AddChild(dutyNumberText);
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer("運転速度"),
                contentColor: MonitorColors.TIMSTitleGrey, x: 524, y: y));
            var standardOperatingSpeedText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(StandardOperatingSpeed)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White, x: 613, y: y);
            standardOperatingSpeedText.IsVisible.Bind(CreateComputed(() =>
                !string.IsNullOrEmpty(StandardOperatingSpeed)));
            AddChild(standardOperatingSpeedText);
        }

        public PropertySlot<string> DutyNumber { get; }
        public PropertySlot<string> StandardOperatingSpeed { get; }
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}