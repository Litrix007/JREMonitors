using System;

namespace JREMonitors.Core.Contexts
{
    public sealed class PropertyKey
    {
        public PropertyKey(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public override string ToString()
        {
            return $"{nameof(PropertyKey)}({Name})";
        }
    }
}