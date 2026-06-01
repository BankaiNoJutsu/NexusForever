using NexusForever.Game.Abstract.Entity;
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
    /// <c>itemData+0x518 + index*4</c>; eval/itemData writer from wire remains blocked — only <c>Glyphs[]</c>
    /// index alignment is implemented here.
    /// </summary>
    internal static class ItemRuneNetworkWire
    {
        public static void Populate(NetworkItem networkItem, IList<ItemRuneSlot> runeSlots, IList<uint> microchipIds)
        {
            foreach (uint microchipId in microchipIds)
                networkItem.Microchips.Add(microchipId);

            foreach (ItemRuneSlot runeSlot in runeSlots)
                networkItem.Glyphs.Add(runeSlot.RuneItem2Id);
        }
    }
}
