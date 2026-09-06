using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.MeterScreen.Base;

namespace JREMonitors.E233.MeterScreen.Background
{
    public class MeterBackgroundRoot1000 : MeterBackgroundRootBase
    {
        public MeterBackgroundRoot1000(RenderContext context) : base(
            context, new RootProperties(
                false,
                new Vector2(133, 177),
                65,
                new Vector2(361, 176),
                65,
                Vector2.Zero,
                0
            ),
            false,
            false,
            CommonRootPropertiesWithoutSafetyAndHldLamps,
            true,
            true,
            -25
        )
        {
            AddChild(new AtcSpeedGaugeBackground(context, 755, 563 + SpeedOffsetY));
        }
    }
}