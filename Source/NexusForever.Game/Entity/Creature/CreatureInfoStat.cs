using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity.Creature
{
    public class CreatureInfoStat : ICreatureInfoStat
    {
        public Stat Stat { get; private set; }
        public float Value { get; private set; }

        public void Initialise(Stat stat, float value)
        {
            Stat  = stat;
            Value = value;
        }
    }
}
