using System.Linq;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using Vortice.Mathematics;

namespace JREMonitors.E233.Constants
{
    public static class Shadows
    {
        public static readonly DropShadow[] TickMarkDrop =
        {
            new DropShadow
                { OffsetX = 1, OffsetY = 1, BlurX = 3, BlurY = 3, Color = new Color4(0, 0, 0, 190) }
        };

        public static readonly OffsetInnerShadow RecessedInnerTopLeft =
            new OffsetInnerShadow(4, 4, 2, Colors.Black.MultiplyAlpha(190f / 255));

        public static readonly OffsetInnerShadow RecessedInnerBottomRight =
            new OffsetInnerShadow(-1.5f, -1.5f, 1.5f, Colors.Black.MultiplyAlpha(150f / 255));

        public static readonly OffsetInnerShadow[] RecessedInner =
        {
            RecessedInnerTopLeft,
            RecessedInnerBottomRight
        };

        public static readonly OffsetInnerShadow[] GaugeRecessedInner =
        {
            RecessedInnerTopLeft,
            RecessedInnerBottomRight,
            RecessedInnerBottomRight.With(offsetY: RecessedInnerBottomRight.OffsetY * -1)
        };

        public static readonly OffsetInnerShadow NormalNeedleInnerTopLeft =
            new OffsetInnerShadow(2, 2, 1, MonitorColors.White.MultiplyAlpha(150f / 255));

        public static readonly OffsetInnerShadow NormalNeedleInnerBottomRight =
            new OffsetInnerShadow(-2.5f, -2.5f, 1, new Color4(0, 0, 0, 190));

        public static readonly OffsetInnerShadow[] NormalNeedleInner =
        {
            NormalNeedleInnerTopLeft,
            NormalNeedleInnerBottomRight
        };

        public static readonly DropShadow GaugeNeedleDrop = new DropShadow
            { OffsetX = 4, OffsetY = 4, BlurX = 3, BlurY = 3, Color = new Color4(0, 0, 0, 190) };

        public static readonly OffsetInnerShadow[] GaugeNeedleInnerBig = NormalNeedleInner;

        public static readonly OffsetInnerShadow[] GaugeNeedleInnerSmall =
            NormalNeedleInner.Select(shadow => shadow / 1.5f).ToArray();
    }
}