using System.Collections.Generic;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.DirectWrite;

namespace JREMonitors.E233.TidScreen.LampGroups
{
    public abstract class TidLampGroupBase<TViewModel> : Widget<TViewModel> where TViewModel : ViewModel
    {
        protected const float AnchorX = 512;

        protected TidLampGroupBase(RenderContext context) : base(context)
        {
        }

        protected TextLayout CreateTextLayout(
            string text,
            float fontSize,
            IList<float> finalOffsetsMainAxis = null,
            float sizeLimit = 0,
            float scaleX = 1,
            float scaleY = 1,
            bool step = false
        )
        {
            return new TextLayout(Context,
                Context.FontManager.GetOrCreateFormat(Fonts.FotSeuratProFamily, fontSize,
                    fontWeight: FontWeight.DemiBold),
                text,
                ContentOrientation.Vertical, flowDirection: ContentFlowDirection.Reverse,
                arrangement: text.Contains("\n")
                    ? step ? ContentArrangement.Step : ContentArrangement.Far
                    : ContentArrangement.Center,
                fixedLineSpacing: text.Contains("\n") ? -fontSize + 3 : 0,
                scaleX: scaleX, scaleY: scaleY,
                finalOffsetsMainAxis: finalOffsetsMainAxis, sizeLimit: sizeLimit,
                useVerticalOverhangMetrics: false);
        }
    }
}