using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.E233.MeterScreen.Background.Base;
using JREMonitors.E233.MeterScreen.Base;

namespace JREMonitors.E233.MeterScreen.Background
{
    public class MeterBackgroundRoot0 : MeterBackgroundRootBase
    {
        public MeterBackgroundRoot0(RenderContext context) : base(
            context,
            new RootProperties(true,
                new Vector2(113, 213),
                65,
                new Vector2(339, 213),
                65,
                Vector2.Zero,
                0),
            false,
            CommonRootPropertiesWithoutSafetyLamps,
            true,
            0
        )
        {
            AddChild(new NormalSpeedGaugeBackground(context));
        }

        protected override void OnSafetyLampsVisible()
        {
            if (!ViewModel.SupportsTasc)
                ApplyLayout(RootPropertiesWithoutSafetyLamps, BoldCenterMinorTicksWithoutSafetyLamps);
            else
                base.OnSafetyLampsVisible();
        }
    }
}