using JREMonitors.Core.Layouts;

namespace JREMonitors.E233.Lamps
{
    public class LampAdjacencyManager : AdjacencyManagerBase<Lamp, bool>
    {
        protected override bool IsActive(bool state)
        {
            return state;
        }
    }
}