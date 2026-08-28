using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXNormalTimeText : Widget
    {
        private readonly BoundsDrawerWidget _text;

        public D01AXNormalTimeText(
            RenderContext context,
            float x,
            float y,
            float secondOffsetY,
            bool boldSeconds,
            IValueSignal<string> hoursAndMinutes,
            IValueSignal<string> seconds,
            IValueSignal<string> customText = null,
            float horizontalAlignment = 0
        ) : base(context, x, y)
        {
            var properties = new List<BitmapScaleDrawer.DrawerProperties>
            {
                new BitmapScaleDrawer.DrawerProperties(
                    this.CreateTIMSTextLayout(
                        documentSource: CreateComputed(() => RichTextParser.Raw(hoursAndMinutes.Value))), 1, 1),
                new BitmapScaleDrawer.DrawerProperties(
                    this.CreateTIMSTextLayout(documentSource: CreateComputed(() => RichTextParser.Raw(seconds.Value)),
                        format: context.FontManager.GetOrCreateFormat(Fonts.MsGothicFamily, 12,
                            fontWeight: boldSeconds ? FontWeight.Bold : FontWeight.Normal)), 1, 1, secondOffsetY)
            };

            if (customText != null)
                properties.Add(new BitmapScaleDrawer.DrawerProperties(
                    this.CreateTIMSTextLayout(
                        documentSource: CreateComputed(() => RichTextParser.Raw(customText.Value))), 1, 1,
                    color: MonitorColors.TIMSTitleGrey));
            _text = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(properties, arrangement: ContentArrangement.Near,
                    horizontalAlignment: horizontalAlignment),
                contentColor: MonitorColors.White);
            AddChild(_text);
        }

        public PropertySlot<Color4> Color => _text.ContentColor;
        public PropertySlot<Color4> BackgroundColor => _text.BackgroundColor;

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}