using SharpGen.Runtime;
using Vortice.DirectWrite;

namespace JREMonitors.Core.Utils.Render
{
    public struct TextEffect
    {
        public readonly int Index;
        public readonly int Length;
        public readonly IUnknown Effect;
        public TextRange Range => new TextRange(Index, Length);

        public TextEffect(int index, int length, IUnknown effect)
        {
            Index = index;
            Length = length;
            Effect = effect;
        }
    }
}