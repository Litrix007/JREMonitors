using JREMonitors.Core.Contexts;
using JREMonitors.E233.MeterScreen.Background.Base;

namespace JREMonitors.E233.MeterScreen.Background
{
    public class MeterBackgroundRoot3000 : MeterBackgroundRootBase
    {
        public MeterBackgroundRoot3000(RenderContext context) : base(
            context,
            CommonRootPropertiesWithoutSafetyLamps,
            true,
            CommonRootPropertiesWithoutSafetyLamps,
            true,
            0
        )
        {
            AddChild(new NormalSpeedGaugeBackground(context));
        }
    }
}