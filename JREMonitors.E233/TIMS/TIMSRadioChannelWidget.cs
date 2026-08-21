using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS
{
    public class TIMSRadioChannelWidget : Widget
    {
        public TIMSRadioChannelWidget(RenderContext context, ScopedRenderContext scopedContext = null, float y = 0) :
            base(context, y: y)
        {
            RadioChannel = CreatePropertySlot<string>(DirtyType.Visual);
            AddChild(new BoundsDrawerWidget(scopedContext ?? context,
                this.CreateTIMSTextDrawer(CreateComputed(() =>
                        new RichTextBuilder()
                            .Append("無線\u3000")
                            .Append(RadioChannel, color: MonitorColors.White)
                            .Append("ch")
                            .Build()), cache: scopedContext != null, context: scopedContext ?? context,
                    horizontalAlignment: 1),
                contentColor: MonitorColors.TIMSTitleGrey, x: 615));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public PropertySlot<string> RadioChannel { get; }
    }
}