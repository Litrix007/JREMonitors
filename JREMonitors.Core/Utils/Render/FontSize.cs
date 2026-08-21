using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils.Render
{
    public struct FontSize
    {
        public int Index;
        public int Length;
        public float Size;

        public FontSize(int index, int length, float size)
        {
            Index = index;
            Length = length;
            Size = size;
        }

        public TextRange Range => new TextRange(Index, Length);
    }
}