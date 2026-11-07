using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 231: native <c>FUN_14049d7b0</c> returns false unless the unit is the local player, then scans the 16-bit id list the client received in the player-create packet (the unlocked
    /// inlaid augmentation ids) for <c>ObjectId</c>. Equal returns "present", NotEqual "absent" (all 241 retail slots are NotEqual with Value 0), other comparisons are false.
    /// Status: native body read; the list's producer (the UnlockInlaidAugment effect) is data-correlated, not verified.
    /// See Decomp/Analysis/PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.EldanAugmentation231)]
    public class PrerequisiteCheckEldanAugmentation231 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            bool unlocked = objectId <= ushort.MaxValue
                && player.InlaidAugmentationManager?.IsUnlocked((ushort)objectId) == true;
            return PrerequisiteCompare.CompareFlag(comparison, unlocked);
        }
    }
}
