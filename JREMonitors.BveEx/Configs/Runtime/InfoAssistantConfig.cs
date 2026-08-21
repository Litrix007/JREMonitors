using System.Numerics;
using System.Windows.Forms;

namespace JREMonitors.BveEx.Configs.Runtime
{
    public class InfoAssistantConfig
    {
        public Vector2 Position { get; set; } = new Vector2(10);
        public int Anchor { get; set; } = (int)(AnchorStyles.Left | AnchorStyles.Top);
    }
}