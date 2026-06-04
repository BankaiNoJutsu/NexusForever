using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.WorldRequirement)]
    public class PrerequisiteCheckWorldRequirement : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint currentWorldId = player.Map?.Entry?.Id ?? 0u;
            return PrerequisiteCompare.Compare(comparison, currentWorldId, objectId);
        }
    }
}
