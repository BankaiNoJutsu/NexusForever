using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity.Creature
{
    public interface ICreatureInfoProperty
    {
        Property Property { get; }
        float Value { get; }

        void Initialise(Property property, float value);
    }
}
