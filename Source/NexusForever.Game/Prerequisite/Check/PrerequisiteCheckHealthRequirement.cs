using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 80: handler table[80] is stub <c>140001ba0</c>; live rows use health-scaled checks.
    /// NF compares health percent on the evaluated unit (same scalar path as type 172).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HealthRequirement)]
    public class PrerequisiteCheckHealthRequirement : IPrerequisiteCheck
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
