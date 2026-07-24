using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity
{
    public interface ISpellPropertyModifier
    {
        List<IPropertyModifier> Alterations { get; }
        uint Priority { get; }
        Property Property { get; }
        uint StackCount { get; }
    }

    /// <summary>
    /// A live property dependency contributed by a spell effect.
    /// </summary>
    public interface ISpellPropertyConversion : ISpellPropertyModifier
    {
        Property SourceProperty { get; }
        float Multiplier { get; }
    }
}
