using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 44: native test <c>FUN_14049ee40</c>. <c>Value</c> is a TargetGroup id (214 of 214 retail slots; <c>ObjectId</c> is 0) and the unit is tested with the
    /// target-group evaluator <c>FUN_1403b4a20</c> (<see cref="TargetGroupCriteriaEvaluator"/>), where 0 means member. Equal meets for a member, NotEqual for a non-member, other
    /// comparisons never meet, and a zero or unknown group id is never a member (so only NotEqual can meet). The subject follows the <c>CreatureDifficulty</c> precedent
    /// (<c>parameters.Target ?? player</c>); a player has no creature id, so creature-list groups only match when a creature target is supplied.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InTargetGroup)]
    public class PrerequisiteCheckInTargetGroup : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckInTargetGroup(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters.Target ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null || value == 0u || gameTableManager.TargetGroup == null)
                return false;

            TargetGroupEntry group = gameTableManager.TargetGroup.GetEntry(value);
            bool member = group != null && TargetGroupCriteriaEvaluator.Evaluate(group, subject, gameTableManager);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => member,
                PrerequisiteComparison.NotEqual => !member,
                _                               => false
            };
        }
    }
}
