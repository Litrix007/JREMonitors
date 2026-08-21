using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils.Render
{
    public struct TextSpacing
    {
        public int Index;
        public int Length;
        public float LeadingSpacing;
        public float TrailingSpacing;
        public float MinAdvanceWidth;
        public TextRange Range => new TextRange(Index, Length);
    }
}