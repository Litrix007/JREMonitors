using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.MeterScreen.Background
{
    public class ElectricalGaugeGroupBackground : Widget
    {
        public ElectricalGaugeGroupBackground(
            RenderContext context,
            Vector2 deviceVoltageGaugePos,
            float deviceVoltageSectorRadius,
            Vector2 catenaryVoltageGaugePos,
            float catenaryVoltageSectorRadius,
            Vector2 currentGaugePos,
            float currentSectorRadius
        ) : base(context)
        {
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}