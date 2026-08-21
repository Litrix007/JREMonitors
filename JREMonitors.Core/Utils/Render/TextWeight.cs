using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils.Render
{
    public readonly struct TextWeight
    {
        public readonly int Index;
        public readonly int Length;
        public readonly FontWeight Weight;

        public TextWeight(int index, int length, FontWeight weight)
        {
            Index = index;
            Length = length;
            Weight = weight;
        }

        public TextRange Range => new TextRange(Index, Length);
    }
}