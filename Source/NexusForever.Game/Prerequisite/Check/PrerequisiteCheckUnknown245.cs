using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 245: evaluator case <c>0xf5</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x698</c> (<c>1404a1790</c>) with the unit, the
    /// comparison and <b>ObjectId</b>. The unit must be a Player or Ghost; then a lookup of ObjectId (<c>1403c16e0</c>) is returned for Equal and negated for NotEqual. Value is never read.
    /// </summary>
    /// <remarks>
    /// The lookup is a dye-colour test, not a tradeskill requirement: all 220 spells gated by type 245 rows are "Learn Dye - ..." spells whose caster-cast prerequisite is (NotEqual, ObjectId), and in 219
    /// of them ObjectId equals DataBits00 of the spell's LearnDyeColor effect. The row therefore reads "this dye is not known yet". The previous non-account path compared a tradeskill tier rank with
    /// Value behind a "needs an NPC target" gate (the Player/Ghost type test read backwards), which failed every Learn Dye cast.
    /// Read natively later the same day: <c>1403c16e0</c> takes the DyeColorRamp row of ObjectId, requires a generic unlock entry of type 1 for the same id, and then requires the ramp's own
    /// prerequisite (row <c>+0x18</c>, set on ramps 355 and 371 only) to pass. That confirms the dye reading. The ramp prerequisite is not evaluated here.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.Unknown245)]
    public class PrerequisiteCheckUnknown245 : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckUnknown245(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters?.AccountItemContext == true)
            {
                uint expected = objectId != 0u ? objectId : value;
                uint current = player.Account?.GenericUnlockManager?.IsDyeUnlocked(expected) == true ? expected : 0u;
                return PrerequisiteCompare.Compare(comparison, current, expected);
            }

            bool known = player.Account?.GenericUnlockManager?.IsDyeUnlocked(objectId) == true;
            return PrerequisiteCompare.CompareFlag(comparison, known);
        }
    }
}
