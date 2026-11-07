using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 69: native test <c>FUN_14049f990</c> (vtable <c>+0x440</c>) returns <c>FUN_14048d530(ObjectId)</c>, the 32-bit field at <c>+0x30</c> of the client challenge record (0 when absent) for Equal and
    /// "the field is 0" for NotEqual; every other comparison is false and <c>Value</c> is not read. The client's own Lua <c>Challenges:IsActivated</c> (<c>FUN_1406859e0</c>) reads that same offset, so the test is
    /// "the challenge is activated". Retail: 396 slots, Equal 303, NotEqual 93, <c>Value</c> 0 in 364. The previous implementation compared the completion count with <c>Value</c>.
    /// See Decomp/Analysis/PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md and PREREQUISITE_CHALLENGE_FIELDS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ChallengeRequirement)]
    public class PrerequisiteCheckChallengeRequirement : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckChallengeRequirement> log;

        public PrerequisiteCheckChallengeRequirement(
            ILogger<PrerequisiteCheckChallengeRequirement> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool activated = objectId <= ushort.MaxValue && player.ChallengeManager.IsChallengeActivated((ushort)objectId);
            return PrerequisiteCompare.CompareFlag(comparison, activated);
        }
    }
}
