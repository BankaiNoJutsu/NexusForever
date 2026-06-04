using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Maps <see cref="IItem.RuneSlots"/> / <see cref="IItem.MicrochipIds"/> onto shared-item wire arrays
    /// (<c>SharedItem_ReadPayload</c> 14008c0d0: 3-bit count at parse-buffer <c>+0x74</c>, 4-bit glyph count at
    /// <c>+0x80</c>). Live bag promotion uses <c>InventoryItem_ApplySharedItemPayload</c> 140569c90 (glyph Item2 ids
    /// → entity <c>+0xbc</c>; microchip ids → entity <c>+0x98</c>). Inspect UI uses
    /// <c>SharedItemRuntime_ApplyParseBuffer</c> 140411b60 (glyphs → runtime <c>+0x8f</c>; microchips via
    /// <c>ItemEval_ApplyItemSpecialToEval</c> → runtime <c>+0x20</c>), then <c>ItemEval_SyncFromSharedItem</c>
    /// 140410300 copies into item-eval. Entity items store glyphs at <c>+0xbc</c> via
    /// <c>InventoryItem_ApplyGlyphsFromWire</c> 14056aba0; <c>InventoryItem_RefreshItemState</c> 14056a430
    /// feeds entity rune fields into <c>ItemEval_ApplyItemSpecialToEval</c>; slot updates call
    /// <c>ItemEval_CommitPendingRuneData</c> 140412ad0 / <c>ItemEval_CommitRuneDataFromLinkedEntity</c>
    /// 140413520 to copy entity <c>+0xbc</c> into shared-runtime <c>[0x8f]</c>. Live
    /// <c>GetRuneSlots</c> reads socket bytes at context <c>+0x388</c>
    /// (<c>param_1+0x71</c> in <c>ItemData_AddRuneSlotsLuaFields</c> 140673b80) and installed Item2 ids at
    /// <c>itemData+0x518 + index*4</c>. <c>ItemEval_CommitRuneDataFromLinkedEntity</c> clears the socket tail when
    /// the random-circuit count is zero, so socket-bearing items also emit a one-entry circuit presence marker.
    /// </summary>
    internal static class ItemRuneNetworkWire
    {
        private const int MaxWireRuneSlots = 8;
        private const int RandomCircuitCountBitOffset = 56;
        private const int RandomGlyphAddedSlotBitOffset = 0;
        private const int RandomGlyphAddedSlotBitWidth = 3;
        private const int RandomGlyphSocketBitOffset = 7;
        private const int RandomGlyphSocketBitWidth = 3;

        public static void Populate(NetworkItem networkItem, IItem item)
        {
            Populate(networkItem, item.RuneSlots, item.MicrochipIds, GetDefinedSocketCount(item));
        }

        public static void Populate(NetworkItem networkItem, IList<ItemRuneSlot> runeSlots, IList<uint> microchipIds, uint definedSocketCount = 0u)
        {
            networkItem.RandomCircuitData = BuildRandomCircuitData(runeSlots);
            networkItem.RandomGlyphData   = BuildRandomGlyphData(runeSlots, definedSocketCount);

            foreach (uint microchipId in microchipIds)
                networkItem.Microchips.Add(microchipId);

            foreach (ItemRuneSlot runeSlot in runeSlots)
                networkItem.Glyphs.Add(runeSlot.RuneItem2Id);
        }

        private static ulong BuildRandomCircuitData(IList<ItemRuneSlot> runeSlots)
        {
            return runeSlots.Count == 0 ? 0ul : 1ul << RandomCircuitCountBitOffset;
        }

        private static uint BuildRandomGlyphData(IList<ItemRuneSlot> runeSlots, uint definedSocketCount)
        {
            uint glyphData = 0u;
            uint addedSlotCount = (uint)Math.Clamp(runeSlots.Count - (int)definedSocketCount, 0, (1 << RandomGlyphAddedSlotBitWidth) - 1);
            glyphData |= addedSlotCount << RandomGlyphAddedSlotBitOffset;

            int slotCount = Math.Min(runeSlots.Count, MaxWireRuneSlots);
            for (int i = 0; i < slotCount; i++)
            {
                uint compactSocketType = ToCompactSocketType(runeSlots[i].Type);
                if (compactSocketType == 0u)
                    continue;

                glyphData |= compactSocketType << (RandomGlyphSocketBitOffset + i * RandomGlyphSocketBitWidth);
            }

            return glyphData;
        }

        private static uint GetDefinedSocketCount(IItem item)
        {
            uint instanceId = item.Info?.Entry?.ItemRuneInstanceId ?? 0u;
            if (instanceId == 0u || GameTableManager.Instance.ItemRuneInstance == null)
                return 0u;

            ItemRuneInstanceEntry instance = GameTableManager.Instance.ItemRuneInstance.GetEntry(instanceId);
            return instance == null ? 0u : Math.Min(instance.DefinedSocketCount, (uint)MaxWireRuneSlots);
        }

        private static uint ToCompactSocketType(RuneType type)
        {
            uint value = (uint)type;
            if (value < (uint)RuneType.Air || value > (uint)RuneType.Fusion)
                return 0u;

            return value - 6u;
        }
    }
}
