using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 143: native test <c>FUN_14049ef70</c> compares the client's world difficulty (singleton field <c>+0x6424</c>, set from the
    /// <c>ServerInstanceSettings.Difficulty</c> field the server sends in <c>Player.InstanceDifficulty</c>) with <c>Value</c>. Equal meets on equality, NotEqual on
    /// inequality, other comparisons never meet; <c>ObjectId</c> is unused. Retail rows are Equal with Value 0 (Normal) or 1 (Veteran).
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Difficulty)]
    public class PrerequisiteCheckWorldDifficulty : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, (uint)player.InstanceDifficulty == value);
    }
}
