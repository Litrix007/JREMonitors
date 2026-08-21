using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXCarDoorGroup : Widget
    {
        private readonly Row _row;

        public D01AXCarDoorGroup(RenderContext context, TIMSVehicleSpec vehicleSpec) : base(context)
        {
            FormationSpec = CreateRelayPropertySlot<TIMSFormationSpec>();
            for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++)
            {
                AllCarDoors[i] = new D01AXCarDoor(context, vehicleSpec);
                var totalWidth = TIMSCarGroup.GetUnitWidth(vehicleSpec);
                var preferredWidth = vehicleSpec.MaxFormationCarCount <= 12 ? 40 : 38;
                var marginWidth = totalWidth - preferredWidth;
                AllCarDoors[i].PreferredWidth.Value = LayoutLength.Absolute(preferredWidth);
                AllCarDoors[i].MarginWidth.Value = marginWidth;
            }

            _row = new Row(context, widgets: AllCarDoors, rowHorizontalAlignment: 0.5f, widgetSpacing: 1,
                positionSnapToPixels: true);
            AnchorX = _row.X;
            AnchorY = _row.Y;
            AddChild(_row);
            WatchEffect(EffectPhase.State, () =>
            {
                if (FormationSpec.Value == null) return;
                var carCount = FormationSpec.Value.CarCount;
                for (var i = 0; i < TIMSFormationSpec.MaxCarCount; i++) AllCarDoors[i].IsVisible.Value = i < carCount;
            });
        }

        public PropertySlot<TIMSFormationSpec> FormationSpec { get; }
        public PropertySlot<float> AnchorX { get; }
        public PropertySlot<float> AnchorY { get; }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public D01AXCarDoor[] AllCarDoors { get; } = new D01AXCarDoor[TIMSFormationSpec.MaxCarCount];
    }
}