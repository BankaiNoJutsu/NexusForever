using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 70: native <c>FUN_14049f9d0</c> looks up the client challenge record with id <c>ObjectId</c> and computes <c>flag = present &amp;&amp; Value &lt; record+0x20</c>, then the boolean tail
    /// (Equal returns the flag, NotEqual its negation, every other comparison is false). Record <c>+0x20</c> is the <c>CurrentTier</c> of <c>ServerChallengeUpdate</c> (the rows are copied word for word into the
    /// record), so the test is "the challenge's current tier is above Value"; an absent record has tier 0. Supersedes the earlier completion-count proxy.
    /// Status: native bodies read; which packet field lands at +0x20 follows the packet/row order. See Decomp/Analysis/PREREQUISITE_CHALLENGE_TIER_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ChallengeTier)]
    public class PrerequisiteCheckChallengeTier : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint tier = objectId <= ushort.MaxValue ? player.ChallengeManager.GetCurrentTier((ushort)objectId) : 0u;
            return PrerequisiteCompare.CompareFlag(comparison, value < tier);
        }
    }
}
