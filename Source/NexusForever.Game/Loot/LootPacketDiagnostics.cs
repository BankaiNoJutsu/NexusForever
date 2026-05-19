using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Static.Loot;
using NLog;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Loot
{
    internal static class LootPacketDiagnostics
    {
        private static readonly ILogger log = LogManager.GetLogger("LootPacketDiagnostics");

        public static void TraceLootNotify(ulong characterId, uint ownerUnitId, uint parentUnitId, bool includeGrantedItems, bool explosion, IReadOnlyCollection<NotifyItemState> items)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "LootDiagnostics notify characterId={0} ownerUnitId={1} parentUnitId={2} parentSemantics={3} includeGrantedItems={4} explosion={5} itemCount={6} items=[{7}]",
                characterId,
                ownerUnitId,
                parentUnitId,
                parentUnitId == ownerUnitId ? "mirrors-owner-pending-evidence" : "custom-parent",
                includeGrantedItems,
                explosion,
                items.Count,
                string.Join("; ", items.Select(FormatItem)));
        }

        public static void TraceLootNotifySuppressed(ulong characterId, uint ownerUnitId, bool includeGrantedItems, bool explosion, int trackedItemCount, int deliveredItemCount)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "LootDiagnostics notify-suppressed characterId={0} ownerUnitId={1} includeGrantedItems={2} explosion={3} trackedItemCount={4} deliveredItemCount={5} reason=no-visible-items",
                characterId,
                ownerUnitId,
                includeGrantedItems,
                explosion,
                trackedItemCount,
                deliveredItemCount);
        }

        public static NotifyItemState CaptureNotifyItemState(LootInstanceItem sourceItem, NetworkLootItem networkItem)
        {
            return new NotifyItemState
            {
                SourceLootUnitId      = sourceItem.Id,
                SourceDelivered       = sourceItem.Delivered,
                WinnerCharacterId     = sourceItem.WinnerCharacterId,
                WinnerGuid            = sourceItem.WinnerGuid,
                LootUnitId            = networkItem.LootUnitId,
                Type                  = networkItem.Type,
                ItemId                = networkItem.ItemId,
                Amount                = networkItem.Amount,
                CanLoot               = networkItem.CanLoot,
                RequiresRoll          = networkItem.RequiresRoll,
                OnlyMasterLootable    = networkItem.OnlyMasterLootable,
                Explosion             = networkItem.Explosion,
                Granted               = networkItem.Granted,
                RollTime              = networkItem.RollTime,
                RandomCircuitData     = networkItem.RandomCircuitData,
                RandomGlyphData       = networkItem.RandomGlyphData,
                ItemQuality2Id        = networkItem.ItemQuality2Id,
                MasterListCount       = networkItem.MasterList?.Count ?? 0
            };
        }

        private static string FormatItem(NotifyItemState item)
        {
            return $"sourceLootUnitId={item.SourceLootUnitId},wireLootUnitId={item.LootUnitId},type={item.Type},itemId={item.ItemId},amount={item.Amount},canLoot={item.CanLoot},requiresRoll={item.RequiresRoll},onlyMasterLootable={item.OnlyMasterLootable},explosion={item.Explosion},granted={item.Granted},rollTime={item.RollTime},randomCircuitData={item.RandomCircuitData},randomGlyphData={item.RandomGlyphData},itemQuality2Id={item.ItemQuality2Id},masterListCount={item.MasterListCount},sourceDelivered={item.SourceDelivered},winnerCharacterId={item.WinnerCharacterId},winnerGuid={item.WinnerGuid}";
        }

        public sealed class NotifyItemState
        {
            public uint SourceLootUnitId { get; init; }
            public bool SourceDelivered { get; init; }
            public ulong WinnerCharacterId { get; init; }
            public uint WinnerGuid { get; init; }
            public uint LootUnitId { get; init; }
            public LootItemType Type { get; init; }
            public uint ItemId { get; init; }
            public uint Amount { get; init; }
            public bool CanLoot { get; init; }
            public bool RequiresRoll { get; init; }
            public bool OnlyMasterLootable { get; init; }
            public bool Explosion { get; init; }
            public bool Granted { get; init; }
            public uint RollTime { get; init; }
            public ulong RandomCircuitData { get; init; }
            public uint RandomGlyphData { get; init; }
            public uint ItemQuality2Id { get; init; }
            public int MasterListCount { get; init; }
        }
    }
}
