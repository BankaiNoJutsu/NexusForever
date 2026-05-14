using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity.Creature
{
    public class CreatureInfoProperty : ICreatureInfoProperty
    {
        public Property Property { get; private set; }
        public float Value { get; private set; }

        public void Initialise(Property property, float value)
        {
            Property = property;
            Value    = value;
        }
    }
}
