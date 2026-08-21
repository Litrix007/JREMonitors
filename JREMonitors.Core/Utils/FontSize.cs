using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils
{
    public struct FontSize
    {
        public int Index;
        public int Length;
        public float Size;
        public TextRange Range => new TextRange(Index, Length);
    }
}