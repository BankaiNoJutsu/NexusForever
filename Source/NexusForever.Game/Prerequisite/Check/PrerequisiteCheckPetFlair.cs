using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 190: evaluator case <c>0xbe</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x658</c> (<c>1404a14d0</c>) with the unit, the comparison
    /// and <b>ObjectId</b>. The unit must be a Player or Ghost. The PetFlair row for ObjectId gives an expected count (0 without a row, 1 when its second unlock bit index is 0, otherwise 2) that is compared
    /// with a per-player count for the same flair (<c>FUN_1403a9c40</c>); Equal is "the counts match", NotEqual the negation, any other comparison false. Value is never read.
    /// </summary>
    /// <remarks>
    /// Read as "the flair is fully unlocked": a PetFlair row has two unlock bit indices and the expected count is how many of them are in use. All 298 retail slots are NotEqual with a PetFlair id in
    /// ObjectId, and they are referenced by 206 AccountItem rows and 36 ItemSpecial rows ("usable while the flair is not unlocked yet"). The previous implementation asked whether a hoverboard
    /// customisation existed for that id, which is a different thing from the unlock.
    /// Not read natively: what <c>FUN_1403a9c40</c> counts. A missing PetFlair row natively compares against 0; the server treats it as not unlocked.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.PetFlair)]
    public class PrerequisiteCheckPetFlair : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            bool unlocked = objectId <= ushort.MaxValue && player.PetCustomisationManager.HasFlair((ushort)objectId);
            return PrerequisiteCompare.CompareFlag(comparison, unlocked);
        }
    }
}
