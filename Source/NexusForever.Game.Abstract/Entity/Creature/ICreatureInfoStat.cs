using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity.Creature
{
    public interface ICreatureInfoStat
    {
        Stat Stat { get; }
        float Value { get; }

        void Initialise(Stat stat, float value);
    }
}
