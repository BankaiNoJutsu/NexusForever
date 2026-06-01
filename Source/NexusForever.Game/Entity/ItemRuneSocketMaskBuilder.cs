using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Builds the client item-eval allowed socket-type bitmask at <c>+0x114</c>
    /// (Prerequisite type 139 / RuneInstall_ValidateRuneMatchesSocket 14040f1b0).
    /// </summary>
    internal static class ItemRuneSocketMaskBuilder
    {
        public static uint BuildAllowedSocketMask(IItem item, IGameTableManager gameTableManager = null)
        {
            uint mask = 0;

            if (item.Info != null && item.Info.Entry.ItemRuneInstanceId != 0u)
            {
                IGameTableManager tables = gameTableManager ?? GameTableManager.Instance;
                ItemRuneInstanceEntry instance = tables.ItemRuneInstance.GetEntry(item.Info.Entry.ItemRuneInstanceId);
                mask |= BuildFromItemRuneInstance(instance);
            }

            mask |= BuildFromRuneSlots(item.RuneSlots);
            mask |= TryGetSocketMaskFromItemSpecial(item, gameTableManager);
            return mask;
        }

        /// <summary>
        /// Client <c>ItemEval_ApplyItemSpecialToEval</c> (14040c310) copies ItemSpecial row <c>+0x10</c> into item-eval <c>+0x114</c>.
        /// SQL import maps that offset to <see cref="ItemSpecialEntry.Spell4IdOnEquip"/>, which is normally a Spell4 FK (e.g. 81886),
        /// not a socket bitmask. Only values that fit the rune-socket bit set are merged here.
        /// </summary>
        private static uint TryGetSocketMaskFromItemSpecial(IItem item, IGameTableManager gameTableManager)
        {
            if (item.Info == null || item.Info.Entry.ItemSpecialId00 == 0u)
                return 0;

            if (gameTableManager == null)
                return 0;

            ItemSpecialEntry special = gameTableManager.ItemSpecial.GetEntry(item.Info.Entry.ItemSpecialId00);
            if (special == null)
                return 0;

            uint candidate = special.Spell4IdOnEquip;
            if (candidate == 0u)
                return 0;

            const uint validSocketBits = 0x7f;
            if (candidate > validSocketBits || (candidate & ~validSocketBits) != 0)
                return 0;

            return candidate;
        }

        public static uint BuildFromRuneSlots(IList<ItemRuneSlot> runeSlots)
        {
            uint mask = 0;
            foreach (ItemRuneSlot slot in runeSlots)
                mask |= ItemRuneSocketTypes.MapRuneTypeToBit(slot.Type);

            return mask;
        }

        public static uint BuildFromItemRuneInstance(ItemRuneInstanceEntry instance)
        {
            if (instance == null || instance.DefinedSocketCount == 0u)
                return 0;

            uint[] definedTypes =
            [
                instance.DefinedSocketType00,
                instance.DefinedSocketType01,
                instance.DefinedSocketType02,
                instance.DefinedSocketType03,
                instance.DefinedSocketType04,
                instance.DefinedSocketType05,
                instance.DefinedSocketType06,
                instance.DefinedSocketType07,
            ];

            uint mask = 0;
            int socketCount = (int)Math.Min(instance.DefinedSocketCount, definedTypes.Length);
            for (int i = 0; i < socketCount; i++)
            {
                if (!ItemRuneSocketTypes.TryToRuneType(definedTypes[i], out RuneType runeType))
                    continue;

                mask |= ItemRuneSocketTypes.MapRuneTypeToBit(runeType);
            }

            return mask;
        }
    }
}
