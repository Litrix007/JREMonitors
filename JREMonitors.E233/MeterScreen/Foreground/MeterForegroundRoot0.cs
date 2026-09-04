using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Base;
using JREMonitors.E233.MeterScreen.Foreground.Base;
using JREMonitors.E233.MeterScreen.LampGroups;
using Vortice;

namespace JREMonitors.E233.MeterScreen.Foreground
{
    public class MeterForegroundRoot0 : MeterForegroundRootBase
    {
        private readonly LampAdjacencyManager _lampAdjacencyManager = new LampAdjacencyManager();

        public MeterForegroundRoot0(RenderContext context) : base(
            context,
            new RootProperties(true,
                new Vector2(113, 213),
                65,
                new Vector2(339, 213),
                65,
                Vector2.Zero,
                0),
            MeterBackgroundRootBase.CommonRootPropertiesWithoutSafetyLamps,
            0
        )
        {
            var normalSpeedGaugeNeedle = new NormalSpeedGaugeNeedle(context);
            InsertChildAfter(normalSpeedGaugeNeedle, InfoButtonGroup);
            var atsStateLampGroupWithTasc = new MeterAtsStateLampGroup(context, 6, 4, true);
            var vehicleStateLampGroupWithTasc = new VehicleStateLampGroup(context,
                new RawRectF(
                    608,
                    122,
                    1010,
                    231),
                10,
                44,
                24,
                20,
                false,
                90,
                LayoutLength.Flex(),
                LayoutLength.Absolute(153),
                4
            );
            var tascLampGroup = new MeterTascLampGroup(context,
                GeometryHelper.CreateGridBounds(147, 11, 9, 4, 60, 32, 2, 7),
                _lampAdjacencyManager, 9, 4, true, false);
            AddLampPanelWhenSafetyLampsVisible(new LampPanel(context, 1023, 432, 310, 455, 95,
                new Widget[] { tascLampGroup, atsStateLampGroupWithTasc, vehicleStateLampGroupWithTasc }));
            var atsStateLampGroupWithoutTasc = new MeterAtsStateLampGroup(context, 6, 4, true);
            var vehicleStateLampGroupWithoutTasc = new VehicleStateLampGroup(context,
                new RawRectF(
                    608,
                    122,
                    1010,
                    231),
                10,
                44,
                24,
                20,
                false,
                90,
                LayoutLength.Flex(),
                LayoutLength.Absolute(153),
                4
            );
            AddLampPanelWhenSafetyLampsVisibleWithoutTasc(CreateSafetyLampsVisiblePanel(context,
                atsStateLampGroupWithoutTasc, vehicleStateLampGroupWithoutTasc));
            WatchEffect(EffectPhase.Visual, () => { _lampAdjacencyManager.UpdateLimits(); });
        }

        protected override void OnSafetyLampsVisible()
        {
            if (!ViewModel.SupportsTasc)
                ApplyLayout(RootPropertiesWithoutSafetyLamps);
            else
                base.OnSafetyLampsVisible();
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}