using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Lamps;
using JREMonitors.E233.MeterScreen.Background;
using JREMonitors.E233.MeterScreen.Base;
using JREMonitors.E233.MeterScreen.Foreground.Atc;
using JREMonitors.E233.MeterScreen.LampGroups;
using Vortice;

namespace JREMonitors.E233.MeterScreen.Foreground
{
    public class MeterForegroundRoot1000 : MeterForegroundRootBase
    {
        private readonly AtcSpeedGaugeForeground _atcSpeedGaugeForeground;
        private readonly LampAdjacencyManager _lampAdjacencyManager = new LampAdjacencyManager();

        public MeterForegroundRoot1000(RenderContext context) : base(context, new RootProperties(
            false,
            new Vector2(133,
                177),
            65,
            new Vector2(361,
                176),
            65,
            Vector2.Zero,
            0,
            -25
        ), MeterBackgroundRootBase.CommonRootPropertiesWithoutSafetyAndHldLamps)
        {
            _atcSpeedGaugeForeground = new AtcSpeedGaugeForeground(context, RootPropertiesWithSafetyLamps.SpeedOffsetY);
            InsertChildAfter(_atcSpeedGaugeForeground, InfoButtonGroup);
            var vehicleStateLampGroupWhenSafetyLampsVisible = new VehicleStateLampGroup(context,
                GeometryHelper.CreateRowBounds(488, 10, 6,
                    new[] { new RowItemWidth(6, 34), new RowItemWidth(1, 38) }, 118),
                6,
                40,
                18,
                0,
                true,
                0,
                LayoutLength.Absolute(38),
                LayoutLength.Absolute(158),
                6,
                24,
                3.5f
            );
            var normalAtcLampGroup = new MeterNormalAtcLampGroup(context,
                new RawRectF(775,
                    10,
                    1015,
                    260),
                _lampAdjacencyManager,
                8,
                10,
                16,
                new MeterNormalAtcLampGroup.InchingActivatedLampProperties(false, 0, "インチング制御中", 14),
                true
            );
            var tascLampGroup = new MeterTascLampGroup(context, new RectangleF(488, 190, 318, 72),
                _lampAdjacencyManager, 9, 4, false, true);
            AddLampPanelWhenSafetyLampsVisible(new LampPanel(context, 1023, 545, 275,
                children: new Widget[]
                    { vehicleStateLampGroupWhenSafetyLampsVisible, tascLampGroup, normalAtcLampGroup }));
            _lampAdjacencyManager.AddConnection(tascLampGroup.TascFailureLamp, normalAtcLampGroup.AtcServiceBrakeLamp,
                AdjacencyDirection.Right, 4);
            _lampAdjacencyManager.AddConnection(tascLampGroup.PlatformDecouplingLamp,
                normalAtcLampGroup.AtcServiceBrakeLamp, AdjacencyDirection.Right, 4);
            WatchEffect(EffectPhase.Visual, () => { _lampAdjacencyManager.UpdateLimits(); });
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}