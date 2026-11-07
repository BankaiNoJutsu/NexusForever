using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 43: native test <c>FUN_14049f3d0</c>. <c>ObjectId</c> is a quest id (1,336 of 1,381 distinct retail ids are Quest2 ids) and <c>Value</c> the objective index (0 to 5). The test is true when the
    /// quest is active, the objective at the index is not complete and it is available (see <see cref="QuestObjectivePrerequisiteHelper.IsObjectiveActive"/>); Equal returns it, NotEqual its negation and
    /// any other comparison is false. Retail: 3,631 slots, Equal 3,497, NotEqual 134, Value 0 to 5.
    /// The previous implementation took <c>ObjectId</c> as a QuestObjective id and compared an active flag with <c>Value</c> (the objective index), which almost never held.
    /// See Decomp/Analysis/PREREQUISITE_OBJECTIVE_ACTIVE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsObjectiveActive)]
    public class PrerequisiteCheckIsObjectiveActive : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckIsObjectiveActive(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool active = QuestObjectivePrerequisiteHelper.IsObjectiveActive(player, gameTableManager, objectId, value);
            return PrerequisiteCompare.CompareFlag(comparison, active);
        }
    }
}
