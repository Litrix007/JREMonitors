using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Foreground.Base;
using JREMonitors.E233.MeterScreen.LampGroups;
using Vortice;

namespace JREMonitors.E233.MeterScreen.Foreground
{
    public class MeterForegroundRoot5000 : MeterForegroundRootBase
    {
        private readonly NormalSpeedGaugeNeedle _normalSpeedGaugeGaugeNeedle;

        public MeterForegroundRoot5000(RenderContext context) : base(
            context,
            MeterBackgroundRootBase.CommonRootPropertiesWithoutSafetyAndHldLamps,
            MeterBackgroundRootBase.CommonRootPropertiesWithoutSafetyAndHldLamps
        )
        {
            _normalSpeedGaugeGaugeNeedle = new NormalSpeedGaugeNeedle(context);
            InsertChildAfter(_normalSpeedGaugeGaugeNeedle, InfoButtonGroup);
            var atsStateLampGroup = new MeterAtsStateLampGroup(context, 6, 4, true);
            var vehicleStateLampGroupWhenSafetyLampsVisible = new VehicleStateLampGroup(context,
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
            AddLampPanelWhenSafetyLampsVisible(new LampPanel(context, 1023, 432, 310,
                children: new Widget[] { atsStateLampGroup, vehicleStateLampGroupWhenSafetyLampsVisible }));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}