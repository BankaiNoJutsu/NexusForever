using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 124: native test <c>FUN_14049fb50</c> reads the 32-bit field at <c>+0x28</c> of the client challenge record with id <c>ObjectId</c> (0 when the record is absent) and compares it with
    /// <c>Value</c> through the shared compare helper (comparison, field, Value). The field is the challenge's completion count: it is the 11th value of <c>ServerChallengeUpdate</c> in record order, and the
    /// type's own name and failure text ("Invalid challenge completion count") agree. Retail: 7 slots, comparisons Equal 3, NotEqual 3, LessThan 1, Values 1 to 45.
    /// Status: Implemented, inferred (record layout from packet order). See Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ChallengeCompletionCount)]
    public class PrerequisiteCheckChallengeCompletionCount : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint count = objectId <= ushort.MaxValue ? player.ChallengeManager.GetCompletionCount((ushort)objectId) : 0u;
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }

    /// <summary>
    /// Prerequisite type 160: native test <c>FUN_14049fbe0</c> reads the 32-bit field at <c>+0x14</c> of the client challenge record with id <c>ObjectId</c> (0 when absent) and compares it with <c>Value</c>.
    /// By the same record order that field is the challenge's current progress count (the 6th value of <c>ServerChallengeUpdate</c>). Retail: 33 slots, Equal 26, <c>Value</c> 0 to 20.
    /// Status: Implemented, inferred (record layout from packet order; the Values look like progress counts). See Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Challenge160)]
    public class PrerequisiteCheckChallengeCurrentCount : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint count = objectId <= ushort.MaxValue ? player.ChallengeManager.GetCurrentCount((ushort)objectId) : 0u;
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
