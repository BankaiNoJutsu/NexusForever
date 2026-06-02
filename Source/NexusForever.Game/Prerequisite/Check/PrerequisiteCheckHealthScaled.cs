using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.HealthScaled)]
    public class PrerequisiteCheckHealthScaled : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            uint currentHealth = entity.MaxHealth > 0u
                ? entity.Health * 100u / entity.MaxHealth
                : entity.Health;
            return PrerequisiteCompare.Compare(comparison, currentHealth, value);
        }
    }
}
