using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 28: evaluator case <c>0x1c</c> reads the 32-bit field at <c>unit+0x2ac</c> and returns it for Equal, "the field is 0" for NotEqual and false for any other comparison; <c>Value</c> is not
    /// read (see <see cref="PrerequisiteCompare.CompareFlag"/>). Retail: 230 slots, <c>Value</c> 0 in 204, NotEqual 159 ("not in combat"), Equal 71. The previous implementation compared the flag with <c>Value</c>,
    /// which made Equal 0 mean "not in combat". See Decomp/Analysis/PREREQUISITE_FLAG_COMPARISON_20261107.md.
    /// Verified native selector: case 0x1c retains the primary unit; Target is relationship context. See Decomp/Analysis/PREREQUISITE_28_PRIMARY_SUBJECT_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InCombat)]
    public class PrerequisiteCheckInCombat : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject != null && PrerequisiteCompare.CompareFlag(comparison, subject.InCombat);
    }
}
