using Vortice.Mathematics;

namespace JREMonitors.E233.Lamps
{
    public class LampColor
    {
        protected LampColor(Color4 color)
        {
            Color = color;
        }

        private Color4 Color { get; }

        public static implicit operator Color4(LampColor color)
        {
            return color.Color;
        }
    }
}