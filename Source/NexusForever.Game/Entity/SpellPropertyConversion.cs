using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public sealed class SpellPropertyConversion : ISpellPropertyConversion
    {
        public Property SourceProperty { get; }
        public Property Property { get; }
        public float Multiplier { get; }
        public uint Priority => 0u;
        public List<IPropertyModifier> Alterations { get; } = [];
        public uint StackCount => 1u;

        public SpellPropertyConversion(Property sourceProperty, Property targetProperty, float multiplier)
        {
            SourceProperty = sourceProperty;
            Property       = targetProperty;
            Multiplier     = multiplier;
        }
    }
}
