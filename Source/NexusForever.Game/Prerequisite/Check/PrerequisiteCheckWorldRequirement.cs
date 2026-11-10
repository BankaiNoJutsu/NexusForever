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
            // Native (evaluator case 0x6d, vtable +0x2c8): Equal and NotEqual against ObjectId only; any other comparison is false (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            return PrerequisiteCompare.CompareFlag(comparison, currentWorldId == objectId);
        }
    }
}
