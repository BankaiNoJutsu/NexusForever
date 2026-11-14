using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 44: native evaluator case 0x2c calls vtable +0x370 (<c>14049ee40</c>) with the primary unit, comparison and <c>Value</c> (a TargetGroup id in all 214 retail slots).
    /// <c>ObjectId</c> is not read (zero in 213 slots). The primary unit is tested with the
    /// target-group evaluator <c>FUN_1403b4a20</c> (<see cref="TargetGroupCriteriaEvaluator"/>), where 0 means member. Equal meets for a member, NotEqual for a non-member, other
    /// comparisons never meet. A zero group id fails both comparisons; an unknown nonzero group is not a member. Both entry points retain their supplied primary subject;
    /// Target is separate relationship context. Evidence tier: Verified native selector; shared group-model limits remain separate. No retail parity claim.
    /// See Decomp/Analysis/PREREQUISITE_44_PRIMARY_SUBJECT_20261114.md.
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
            => MeetsUnit(player, comparison, value, objectId, parameters);

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
