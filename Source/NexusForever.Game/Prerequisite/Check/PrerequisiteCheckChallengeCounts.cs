using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 124: native test <c>FUN_14049fb50</c> reads the 32-bit field at <c>+0x28</c> of the client challenge record with id <c>ObjectId</c> (0 when the record is absent) and compares it with
    /// <c>Value</c> through the shared compare helper (comparison, field, Value). The field is the challenge's completion count: the client's own Lua <c>Challenges:GetCompletionCount</c> (<c>FUN_140685640</c>) reads that
    /// same offset, and the type's name and failure text ("Invalid challenge completion count") agree. Retail: 7 slots, comparisons Equal 3, NotEqual 3, LessThan 1, Values 1 to 45.
    /// Status: Implemented (field offset confirmed by the Lua accessor). See Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
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
    /// Prerequisite type 125: native test <c>FUN_14049fac0</c> reads the 32-bit field at <c>+0x34</c> of the client challenge record with id <c>ObjectId</c> (0 when absent) and returns it for Equal, "the field is 0" for
    /// NotEqual and false for every other comparison. The client's own Lua <c>Challenges:IsInCooldown</c> (<c>FUN_140685ad0</c>) tests that same field, so it is the on-cooldown flag.
    /// Retail: 153 slots, NotEqual 145, Equal 8. An untouched challenge and an absent record are both not on cooldown, so the server's lack of a record for a challenge does not change the result.
    /// See Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Challenge125)]
    public class PrerequisiteCheckChallengeOnCooldown : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool onCooldown = objectId <= ushort.MaxValue && player.ChallengeManager.IsChallengeOnCooldown((ushort)objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => onCooldown,
                PrerequisiteComparison.NotEqual => !onCooldown,
                _                               => false
            };
        }
    }

    /// <summary>
    /// Prerequisite type 160: native test <c>FUN_14049fbe0</c> reads the 32-bit field at <c>+0x14</c> of the client challenge record with id <c>ObjectId</c> (0 when absent) and compares it with <c>Value</c>.
    /// The client's Lua <c>Challenges:GetCurrentCount</c> (<c>FUN_140685210</c>) reads that same offset, so it is the challenge's current progress count. Retail: 33 slots, Equal 26, <c>Value</c> 0 to 20.
    /// Status: Implemented (field offset confirmed by the Lua accessor). See Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
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
