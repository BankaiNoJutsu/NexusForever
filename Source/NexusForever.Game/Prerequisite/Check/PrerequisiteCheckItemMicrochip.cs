using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemMicrochip)]
    public class PrerequisiteCheckItemMicrochip : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemMicrochip(IGameTableManager gameTableManager = null)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            // Native (evaluator case 0x8b, 1404a0e10) receives the item context, the comparison and Value: Value is mapped to a socket bit (ids 7 to 13, anything else no bit) and tested against
            // the item mask; Equal returns the flag, NotEqual its negation (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md). ObjectId is never read. Both retail slots (Equal, Value 1 and 2)
            // map to no bit and are therefore never met; the previous implementation mapped ObjectId and treated Value as an installed-rune count.
            // Prerequisite_MapMicrochipIdToBit (14049bdc0) only knows ids 7 to 13; the compact slot bytes 1 to 7 that ItemRuneSocketTypes.TryToRuneType also accepts are not ids here.
            uint mask = value is >= (uint)RuneType.Air and <= (uint)RuneType.Fusion
                ? ItemRuneSocketTypes.MapRuneTypeToBit((RuneType)value)
                : 0u;

            // a Value that maps to no bit can never be present; it still answers NotEqual
            bool present = mask != 0u
                && (ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(parameters.Item, gameTableManager) & mask) != 0u;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => present,
                PrerequisiteComparison.NotEqual => !present,
                _                               => false
            };
        }

        private static uint GetCountForObjectIdZero(IItem item)
        {
            if (item.MicrochipIds.Count > 0)
                return (uint)item.MicrochipIds.Count;

            uint installedRunes = 0;
            foreach (ItemRuneSlot slot in item.RuneSlots)
            {
                if (slot.RuneItem2Id != 0u)
                    installedRunes++;
            }

            return installedRunes;
        }
    }
}
