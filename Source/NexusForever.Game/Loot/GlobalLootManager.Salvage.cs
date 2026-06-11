using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Static;
using NetworkItemLocation = NexusForever.Network.World.Message.Model.Shared.ItemLocation;

namespace NexusForever.Game.Loot
{
    public partial class GlobalLootManager
    {
        private void BuildItemSalvage(
            ItemSalvageModel itemSalvageModel,
            ref int skippedRows,
            ref int exactRows,
            ref int typeLevelRows)
        {
            if (!IsValidItemSalvageRow(itemSalvageModel, itemManager, gameTableManager))
            {
                skippedRows++;
                return;
            }

            switch (itemSalvageModel.Purpose)
            {
                case ItemSalvagePurpose.ExactItem:
                    AddItemSalvage(itemSalvageByItem, itemSalvageModel.SourceItemId, itemSalvageModel);
                    exactRows++;
                    break;
                case ItemSalvagePurpose.ClientTypeLevel:
                    AddItemSalvage(
                        itemSalvageByTypeLevel,
                        (itemSalvageModel.SourceItem2TypeId, itemSalvageModel.SourceLevel),
                        itemSalvageModel);
                    typeLevelRows++;
                    break;
            }
        }

        private static void AddItemSalvage<TKey>(
            Dictionary<TKey, List<ItemSalvageModel>> itemSalvage,
            TKey key,
            ItemSalvageModel itemSalvageModel)
            where TKey : notnull
        {
            if (!itemSalvage.TryGetValue(key, out List<ItemSalvageModel> items))
            {
                items = [];
                itemSalvage.Add(key, items);
            }

            items.Add(itemSalvageModel);
        }

        private static bool IsValidItemSalvageRow(ItemSalvageModel itemSalvageModel, IItemManager itemManager, IGameTableManager gameTableManager)
        {
            if (itemSalvageModel == null)
                return false;

            if (itemSalvageModel.StaticId == 0u || itemSalvageModel.Probability <= 0f)
                return false;

            if (!CanDeliverLootItem(new GeneratedLootItem(
                (LootItemType)itemSalvageModel.Type,
                itemSalvageModel.StaticId,
                Math.Max(1u, itemSalvageModel.MinCount)),
                itemManager,
                gameTableManager))
                return false;

            return itemSalvageModel.Purpose switch
            {
                ItemSalvagePurpose.ExactItem       => itemSalvageModel.SourceItemId != 0u,
                ItemSalvagePurpose.ClientTypeLevel => itemSalvageModel.SourceItem2TypeId != 0u && itemSalvageModel.SourceLevel != 0u,
                _                                  => false
            };
        }

        public bool TrySalvageItem(IPlayer looter, IItem salvagedItem, out string reason)
        {
            reason = string.Empty;
            if (looter == null)
            {
                reason = "no-looter";
                return false;
            }

            if (salvagedItem?.Info == null)
            {
                reason = "missing-item";
                return false;
            }

            if (!TryGenerateItemSalvageLoot(salvagedItem, out IReadOnlyList<GeneratedLootItem> items, out reason))
            {
                log.Trace($"Item salvage failed during generation for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}.");
                return false;
            }

            if (!CanDeliverGeneratedLoot(looter, items, out reason))
            {
                log.Trace($"Item salvage failed during delivery validation for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (!CanDeliverGeneratedItemLoot(looter, items, itemManager, out reason))
            {
                log.Trace($"Item salvage failed during delivery preflight for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            IItem deletedItem;
            try
            {
                deletedItem = looter.Inventory.ItemDelete(new NetworkItemLocation
                {
                    Location = salvagedItem.Location,
                    BagIndex = salvagedItem.BagIndex
                }, 1u, ItemUpdateReason.Salvage);
            }
            catch (InvalidPacketValueException)
            {
                reason = "item-delete-failed";
                log.Trace($"Item salvage failed while deleting source item for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }
            catch (ArgumentException)
            {
                reason = "item-delete-failed";
                log.Trace($"Item salvage failed while deleting source item for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (deletedItem == null)
            {
                reason = "item-delete-failed";
                log.Trace($"Item salvage failed while deleting source item for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (!TryDeliverGeneratedItemLoot(looter, items, looter.Guid, playerManager, itemManager, gameTableManager))
            {
                reason = "loot-delivery-failed";
                log.Warn($"Item salvage failed during final delivery after source item deletion for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            log.Trace($"Item salvage succeeded for player {looter.CharacterId}, item {salvagedItem.Info.Entry.Id}: generatedItems=[{FormatGeneratedLootItems(items)}].");
            return true;
        }

        private bool TryGenerateItemSalvageLoot(IItem salvagedItem, out IReadOnlyList<GeneratedLootItem> items, out string reason)
        {
            items  = [];
            reason = string.Empty;

            if (salvagedItem?.Info?.Entry == null)
            {
                reason = "missing-item";
                return false;
            }

            Item2Entry itemEntry = salvagedItem.Info.Entry;
            if (itemSalvageByItem.TryGetValue(itemEntry.Id, out List<ItemSalvageModel> exactItemSalvage))
                return TryGenerateItemSalvageLoot(itemEntry.Id, exactItemSalvage, itemManager, gameTableManager, out items, out reason);

            uint salvageLevel = GetClientSalvageLevel(itemEntry);
            if (itemEntry.Item2TypeId != 0u
                && salvageLevel != 0u
                && itemSalvageByTypeLevel.TryGetValue((itemEntry.Item2TypeId, salvageLevel), out List<ItemSalvageModel> typeLevelItemSalvage))
            {
                return TryGenerateItemSalvageLoot(itemEntry.Id, typeLevelItemSalvage, itemManager, gameTableManager, out items, out reason);
            }

            reason = $"missing-item-salvage:{itemEntry.Id}";
            return false;
        }

        private static bool TryGenerateItemSalvageLoot(
            uint itemId,
            IReadOnlyList<ItemSalvageModel> itemSalvage,
            IItemManager itemManager,
            IGameTableManager gameTableManager,
            out IReadOnlyList<GeneratedLootItem> items,
            out string reason)
        {
            items  = [];
            reason = string.Empty;

            double totalProbability = itemSalvage.Sum(i => Math.Max(0f, i.Probability));
            if (totalProbability <= 0d)
            {
                reason = $"empty-item-salvage:{itemId}";
                return false;
            }

            double roll = Random.Shared.NextDouble() * totalProbability;
            double currentProbability = 0d;
            foreach (ItemSalvageModel itemSalvageModel in itemSalvage)
            {
                if (itemSalvageModel.Probability <= 0f)
                    continue;

                currentProbability += itemSalvageModel.Probability;
                if (roll > currentProbability)
                    continue;

                if (!TryCreateGeneratedItemSalvageLoot(itemSalvageModel, itemManager, gameTableManager, out GeneratedLootItem item, out reason))
                    return false;

                items = [item];
                return true;
            }

            reason = $"empty-item-salvage:{itemId}";
            return false;
        }

        private static bool TryCreateGeneratedItemSalvageLoot(
            ItemSalvageModel itemSalvageModel,
            IItemManager itemManager,
            IGameTableManager gameTableManager,
            out GeneratedLootItem item,
            out string reason)
        {
            LootItemType type = (LootItemType)itemSalvageModel.Type;
            uint minimum = Math.Max(1u, itemSalvageModel.MinCount);
            uint maximum = Math.Max(minimum, itemSalvageModel.MaxCount);
            uint count = minimum == maximum
                ? minimum
                : (uint)Random.Shared.NextInt64(minimum, (long)maximum + 1L);

            item = new GeneratedLootItem(type, itemSalvageModel.StaticId, count);
            if (!CanDeliverLootItem(item, itemManager, gameTableManager))
            {
                reason = $"invalid-loot-item:{item.Type}:{item.StaticId}";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static uint GetClientSalvageLevel(Item2Entry itemEntry)
        {
            if (itemEntry.PowerLevel != 0u)
                return itemEntry.PowerLevel;
            if (itemEntry.RequiredItemLevel != 0u)
                return itemEntry.RequiredItemLevel;
            return itemEntry.RequiredLevel;
        }
    }
}
