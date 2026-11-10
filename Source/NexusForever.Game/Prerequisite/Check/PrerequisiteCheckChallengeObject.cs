using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <remarks>
    /// Re-read 2026-11-10 (evaluator case 0xb0, vtable +0x470, <c>14049fc70</c>; see Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md): the handler receives the primary unit, the comparison, ObjectId and the second unit. Value is never read.
    /// When the primary unit is a player or ghost and the second unit is absent or also a player or ghost, NotEqual is true and everything else false without a lookup. Otherwise a flag from the challenge
    /// service lookup of ObjectId is returned for Equal and negated for NotEqual; other comparisons are false. The previous implementation compared the flag with Value, which inverted 24 of the 25 retail slots
    /// (Value 0). That the flag means "challenge activated" is the existing server reading, not re-derived. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.ChallengeObject)]
    public class PrerequisiteCheckChallengeObject : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckChallengeObject(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native gate: a player or ghost primary unit with no second unit, or with a player or ghost second unit, never reaches the lookup.
            if (parameters?.Target == null || parameters.Target.Type is EntityType.Player or EntityType.Ghost)
                return comparison == PrerequisiteComparison.NotEqual;

            bool activated = gameTableManager.Challenge.GetEntry(objectId) != null
                && player.ChallengeManager.IsChallengeActivated((ushort)objectId);
            return PrerequisiteCompare.CompareFlag(comparison, activated);
        }
    }
}
