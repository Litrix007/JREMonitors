using JREMonitors.Core.Utils;
using Vortice.Mathematics;

namespace JREMonitors.E233.Lamps
{
    public class LampBorderColor : LampColor
    {
        public static readonly LampBorderColor HoldSpeedGreen =
            new LampBorderColor("#0F5834".ToColor4());

        public static readonly LampBorderColor EbRedOff =
            new LampBorderColor("#560F0E".ToColor4());

        public static readonly LampBorderColor
            EbRedOn = new LampBorderColor("#B00504".ToColor4());

        private LampBorderColor(Color4 color) : base(color)
        {
        }
    }
}