using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 149: native <c>FUN_14049c840</c> computes <c>state = (unit+0x1e4 != 0) || (unit+0x2ac != 0)</c> and compares it with <c>Value</c>: Equal <c>state == Value</c>, NotEqual
    /// <c>state != Value</c>. Verified native case <c>0x95</c> retains the primary unit. Field <c>+0x1e4</c> is written by the <c>ServerUnitInUse</c> (0x0755) consumer;
    /// field <c>+0x2ac</c> by the combat setter reached from 0x089A and read by Lua IsInCombat. Source sends these flags from IsBusy/InCombat. Other comparisons fail; ObjectId/relationship are unread.
    /// Writer/reader bindings were re-derived by the lead; see Decomp/Analysis/PREREQUISITE_149_PRIMARY_SUBJECT_20261114.md. No retail timing/authority claim.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.State)]
    public class PrerequisiteCheckEntityState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            uint state = subject.IsBusy || subject.InCombat ? 1u : 0u;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => state == value,
                PrerequisiteComparison.NotEqual => state != value,
                _                               => false
            };
        }
    }
}
