using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS
{
    public class TIMSScreenTitle : Widget
    {
        private readonly BoundsDrawerWidget _idText;
        private readonly BoundsDrawerWidget _titleText;


        public TIMSScreenTitle(RenderContext context, string id, string title) : base(context, 21, 65)
        {
            Id = CreatePropertySlot(DirtyType.Visual, id);
            Title = CreatePropertySlot(DirtyType.Visual, title);
            _idText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(Id))),
                contentColor: MonitorColors.TIMSTitleGrey);
            AddChild(_idText);
            _titleText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(Title)), 2),
                contentColor: MonitorColors.TIMSTitleGreen, x: 78);
            AddChild(_titleText);
        }

        public PropertySlot<string> Id { get; }
        public PropertySlot<string> Title { get; }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
    }
}