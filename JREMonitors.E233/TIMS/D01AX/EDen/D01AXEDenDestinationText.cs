using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenDestinationText : Widget
    {
        public D01AXEDenDestinationText(RenderContext context, ScopedRenderContext scopedContext,
            IValueSignal<string> name) : base(context, y: 154)
        {
            var text = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(name.Value)),
                    context: scopedContext), contentColor: MonitorColors.White);
            AddChild(text);
            AddChild(new BoundsDrawerWidget(context, this.CreateTIMSTextDrawer("行"),
                contentColor: MonitorColors.TIMSTitleGrey, x: 75));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}