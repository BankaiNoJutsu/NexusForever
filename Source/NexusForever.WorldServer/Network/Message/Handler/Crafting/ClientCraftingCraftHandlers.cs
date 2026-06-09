using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingSimpleCraftHandler : IMessageHandler<IWorldSession, ClientCraftingSimpleCraft>
    {
        private readonly ILogger<ClientCraftingSimpleCraftHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly IGlobalLootManager lootManager;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingSimpleCraftHandler(
            ILogger<ClientCraftingSimpleCraftHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            IGlobalLootManager lootManager,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.gameTableManager             = gameTableManager;
            this.itemManager                  = itemManager;
            this.lootManager                  = lootManager;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingSimpleCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.LogBlockedStationServiceKeyDiagnostic(log, schematic, craft.CraftingStationUnitId);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, lootManager, craftingModifierSessionStore, schematic, 1u, craft.CraftingStationUnitId, out string reason))
            {
                log.LogDebug("Completed simple craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}.",
                    session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id);
                return;
            }

            log.LogDebug("Rejected simple craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, reason {Reason}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, reason);
            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingComplexCraftHandler : IMessageHandler<IWorldSession, ClientCraftingComplexCraft>
    {
        private readonly ILogger<ClientCraftingComplexCraftHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly IGlobalLootManager lootManager;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingComplexCraftHandler(
            ILogger<ClientCraftingComplexCraftHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            IGlobalLootManager lootManager,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.gameTableManager             = gameTableManager;
            this.itemManager                  = itemManager;
            this.lootManager                  = lootManager;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingComplexCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.LogBlockedStationServiceKeyDiagnostic(log, schematic, craft.CraftingStationUnitId);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.PowerCoreItem2Id);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, lootManager, craftingModifierSessionStore, schematic, 1u, craft.CraftingStationUnitId, out string reason, craft.PowerCoreItem2Id))
            {
                log.LogDebug("Completed complex craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, powerCore {PowerCoreItem2Id}, charges {ChargeCount}.",
                    session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.PowerCoreItem2Id, craft.ChargeCounts?.Length ?? 0);
                return;
            }

            log.LogDebug("Rejected complex craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, powerCore {PowerCoreItem2Id}, charges {ChargeCount}, reason {Reason}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.PowerCoreItem2Id, craft.ChargeCounts?.Length ?? 0, reason);
            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItem>
    {
        private readonly ILogger<ClientCraftingCraftItemHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly IGlobalLootManager lootManager;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingCraftItemHandler(
            ILogger<ClientCraftingCraftItemHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            IGlobalLootManager lootManager,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.gameTableManager             = gameTableManager;
            this.itemManager                  = itemManager;
            this.lootManager                  = lootManager;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItem craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.LogBlockedStationServiceKeyDiagnostic(log, schematic, craft.CraftingStationUnitId);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.CatalystItem2Id);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, lootManager, craftingModifierSessionStore, schematic, craft.SchematicCount, craft.CraftingStationUnitId, out string reason, craft.CatalystItem2Id))
            {
                log.LogDebug("Completed craft-item request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}.",
                    session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount);
                return;
            }

            log.LogDebug("Rejected craft-item request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}, catalyst {CatalystItem2Id}, reason {Reason}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount, craft.CatalystItem2Id, reason);
            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemAutoCraftHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItemAutoCraft>
    {
        private readonly ILogger<ClientCraftingCraftItemAutoCraftHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly IGlobalLootManager lootManager;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingCraftItemAutoCraftHandler(
            ILogger<ClientCraftingCraftItemAutoCraftHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            IGlobalLootManager lootManager,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.gameTableManager             = gameTableManager;
            this.itemManager                  = itemManager;
            this.lootManager                  = lootManager;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItemAutoCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.LogBlockedStationServiceKeyDiagnostic(log, schematic, craft.CraftingStationUnitId);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, lootManager, craftingModifierSessionStore, schematic, craft.SchematicCount, craft.CraftingStationUnitId, out string reason))
            {
                log.LogDebug("Completed auto-craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}.",
                    session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount);
                return;
            }

            log.LogDebug("Rejected auto-craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}, reason {Reason}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount, reason);
            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    internal static class CraftingCraftRequestHelper
    {
        private const uint AllTradeskillsStationId = uint.MaxValue;
        private const uint AllTradeskillsStationIdSqlImport = int.MaxValue;

        private readonly record struct MaterialRequirement(uint Item2Id, uint Count);

        private readonly record struct MaterialDebit(uint Item2Id, ushort MaterialId, uint SatchelCount, uint InventoryCount);

        private readonly record struct ItemDebit(uint Item2Id, uint Count, ItemUpdateReason Reason);

        public static TradeskillSchematic2Entry GetSchematic(IGameTableManager gameTableManager, uint tradeskillSchematic2Id)
        {
            TradeskillSchematic2Entry schematic = gameTableManager.TradeskillSchematic2?.GetEntry(tradeskillSchematic2Id);
            if (schematic == null)
                throw new InvalidPacketValueException();

            return schematic;
        }

        public static void ValidateItem(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            if (gameTableManager.Item?.GetEntry(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        /// <summary>
        /// Logs when a schematic resolves to a native station service key whose descriptive name remains blocked.
        /// Does not change validation or emit behavior.
        /// </summary>
        public static void LogBlockedStationServiceKeyDiagnostic(
            ILogger logger,
            TradeskillSchematic2Entry schematic,
            uint craftingStationUnitId)
        {
            uint serviceKey = CraftingStationServiceKey.GetForSchematic(schematic.TradeSkillId, schematic.Tier, schematic.Flags);
            if (!IsBlockedStationServiceKey(serviceKey))
                return;

            logger.LogDebug(
                "Crafting station service key {ServiceKey} (native name blocked) schematic {SchematicId} tradeSkill {TradeSkillId} tier {Tier} flags {Flags} station {StationUnitId}.",
                serviceKey,
                schematic.Id,
                schematic.TradeSkillId,
                schematic.Tier,
                schematic.Flags,
                craftingStationUnitId);
        }

        private static bool IsBlockedStationServiceKey(uint serviceKey)
        {
            return serviceKey == CraftingStationServiceKey.DefaultSchematic
                || serviceKey == CraftingStationServiceKey.RunecraftingTradeSkill
                || serviceKey == CraftingStationServiceKey.TierZeroFlaggedSchematic;
        }

        public static bool TryCompleteFixedRecipe(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            IGlobalLootManager lootManager,
            ICraftingModifierSessionStore craftingModifierSessionStore,
            TradeskillSchematic2Entry schematic,
            uint craftCount,
            uint craftingStationUnitId,
            out string reason,
            uint extraConsumeItem2Id = 0u)
        {
            reason = string.Empty;

            if (session.Player == null)
            {
                reason = "no-player";
                return false;
            }

            if (craftCount == 0u)
            {
                reason = "zero-count";
                return false;
            }

            if (!TryValidateCraftingStation(session.Player, schematic, craftingStationUnitId, out reason))
                return false;

            if (schematic.TradeSkillId != 0u)
            {
                TradeskillType requiredTradeskill = (TradeskillType)schematic.TradeSkillId;
                if (!Enum.IsDefined(requiredTradeskill))
                {
                    reason = $"invalid-tradeskill:{schematic.TradeSkillId}";
                    return false;
                }

                if (!session.Player.HasTradeskill(requiredTradeskill))
                {
                    reason = $"missing-tradeskill:{schematic.TradeSkillId}";
                    return false;
                }
            }

            bool hasDirectOutput = schematic.Item2IdOutput != 0u && schematic.OutputCount != 0u;
            IItemInfo outputInfo = null;
            uint totalOutputCount = 0u;
            IReadOnlyList<GeneratedLootItem> generatedLoot = [];
            uint item2IdCrafted = 0u;

            if (!hasDirectOutput && schematic.LootId == 0u)
            {
                reason = "missing-output";
                return false;
            }

            if (hasDirectOutput && !TryMultiply(schematic.OutputCount, craftCount, out totalOutputCount))
            {
                reason = "output-count-overflow";
                return false;
            }

            if (hasDirectOutput)
            {
                outputInfo = itemManager.GetItemInfo(schematic.Item2IdOutput);
                if (outputInfo == null)
                {
                    reason = "invalid-output-item";
                    return false;
                }

                if (!CanCreateInventoryItem(session.Player.Inventory, outputInfo, totalOutputCount))
                {
                    reason = "inventory-full";
                    session.Player.SendGenericError(GenericError.ItemInventoryFull);
                    return false;
                }

                item2IdCrafted = outputInfo.Id;
            }
            else
            {
                if (!lootManager.TryGenerateLoot(schematic.LootId, session.Player, craftCount, out generatedLoot, out reason))
                {
                    reason = $"loot-output:{reason}";
                    return false;
                }

                if (!lootManager.CanDeliverGeneratedLoot(session.Player, generatedLoot, out reason))
                {
                    if (reason == "inventory-full")
                        session.Player.SendGenericError(GenericError.ItemInventoryFull);

                    reason = $"loot-delivery:{reason}";
                    return false;
                }

                item2IdCrafted = generatedLoot
                    .FirstOrDefault(i => i.Type == LootItemType.StaticItem)
                    .StaticId;
            }

            var debits = new List<MaterialDebit>();
            if (extraConsumeItem2Id != 0u)
            {
                if (!TryBuildMaterialDebit(session.Player, gameTableManager, extraConsumeItem2Id, craftCount, out MaterialDebit extraDebit))
                {
                    reason = $"missing-extra-item:{extraConsumeItem2Id}:{craftCount}";
                    return false;
                }

                debits.Add(extraDebit);
            }

            foreach (MaterialRequirement requirement in GetMaterialRequirements(schematic))
            {
                if (!TryMultiply(requirement.Count, craftCount, out uint totalMaterialCount))
                {
                    reason = $"material-count-overflow:{requirement.Item2Id}";
                    return false;
                }

                if (!TryBuildMaterialDebit(session.Player, gameTableManager, requirement.Item2Id, totalMaterialCount, out MaterialDebit debit))
                {
                    reason = $"missing-material:{requirement.Item2Id}:{totalMaterialCount}";
                    return false;
                }

                debits.Add(debit);
            }

            var modifierDebits = new List<ItemDebit>();
            if (!craftingModifierSessionStore.TryBuildModifierItemCounts(session.Player, gameTableManager, schematic, out IReadOnlyDictionary<uint, uint> modifierItemCounts, out reason))
                return false;

            foreach ((uint item2Id, uint count) in modifierItemCounts)
                modifierDebits.Add(new ItemDebit(item2Id, count, ItemUpdateReason.TradeskillAdditiveCost));

            foreach (MaterialDebit debit in debits)
                ApplyMaterialDebit(session.Player, debit);

            foreach (ItemDebit debit in modifierDebits)
                session.Player.Inventory.ItemDelete(debit.Item2Id, debit.Count, debit.Reason);

            if (hasDirectOutput)
                session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, outputInfo, totalOutputCount, ItemUpdateReason.Crafting);
            else
                lootManager.GiveGeneratedLoot(session.Player, generatedLoot, session.Player.Guid, sendGrantedNotify: true, parentUnitId: craftingStationUnitId);

            GrantCraftingAchievements(session.Player, hasDirectOutput
                ? [new GeneratedLootItem(LootItemType.StaticItem, outputInfo.Id, totalOutputCount)]
                : generatedLoot);

            uint earnedXp = GrantCraftingXp(session.Player, gameTableManager, schematic, craftCount);
            CraftingQuestObjectiveUpdater.OnSchematicCrafted(session.Player, schematic.Id, craftCount);
            SendCraftSuccess(session, schematic, item2IdCrafted, earnedXp);
            craftingModifierSessionStore.ClearModifiers(session.Player);
            return true;
        }

        public static bool TryValidateCraftingStation(IPlayer player, TradeskillSchematic2Entry schematic, uint craftingStationUnitId, out string reason)
        {
            reason = string.Empty;

            if (craftingStationUnitId == 0u)
                return true;

            IWorldEntity station = player.Map?.GetEntity<IWorldEntity>(craftingStationUnitId);
            if (station == null)
            {
                reason = $"unknown-station:{craftingStationUnitId}";
                return false;
            }

            uint stationTradeskillId = station.CreatureEntry?.TradeSkillIdStation ?? 0u;
            if (stationTradeskillId == 0u)
            {
                reason = $"not-crafting-station:{craftingStationUnitId}";
                return false;
            }

            if (schematic.TradeSkillId != 0u
                && !IsAllTradeskillsStation(stationTradeskillId)
                && stationTradeskillId != schematic.TradeSkillId)
            {
                reason = $"station-tradeskill-mismatch:{craftingStationUnitId}:{stationTradeskillId}:{schematic.TradeSkillId}";
                return false;
            }

            return true;
        }

        private static bool IsAllTradeskillsStation(uint stationTradeskillId)
        {
            return stationTradeskillId == AllTradeskillsStationId
                || stationTradeskillId == AllTradeskillsStationIdSqlImport;
        }

        public static bool TryValidateAnyCraftingStation(IPlayer player, uint craftingStationUnitId, out string reason)
        {
            reason = string.Empty;

            if (player == null)
            {
                reason = "no-player";
                return false;
            }

            if (craftingStationUnitId == 0u)
            {
                reason = "zero-station";
                return false;
            }

            IWorldEntity station = player.Map?.GetEntity<IWorldEntity>(craftingStationUnitId);
            if (station == null)
            {
                reason = $"unknown-station:{craftingStationUnitId}";
                return false;
            }

            if ((station.CreatureEntry?.TradeSkillIdStation ?? 0u) == 0u)
            {
                reason = $"not-crafting-station:{craftingStationUnitId}";
                return false;
            }

            return true;
        }

        public static void SendCraftFailure(IWorldSession session, TradeskillSchematic2Entry schematic)
        {
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = false,
                TradeskillSchematic2IdCrafted = schematic.Id
            });
        }

        private static void SendCraftSuccess(IWorldSession session, TradeskillSchematic2Entry schematic, uint item2IdCrafted, uint earnedXp)
        {
            SendCraftFinish(session, schematic, pass: true, item2IdCrafted, earnedXp, CraftingDiscovery.Success);
        }

        private static void SendCraftFinish(
            IWorldSession session,
            TradeskillSchematic2Entry schematic,
            bool pass,
            uint item2IdCrafted,
            uint earnedXp,
            CraftingDiscovery hotOrCold,
            CraftingDirection direction = CraftingDirection.None)
        {
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = pass,
                TradeskillSchematic2IdCrafted = schematic.Id,
                Item2IdCrafted = item2IdCrafted,
                HotOrCold = hotOrCold,
                Direction = direction,
                EarnedXp = earnedXp
            });
        }

        private static uint GrantCraftingXp(IPlayer player, IGameTableManager gameTableManager, TradeskillSchematic2Entry schematic, uint craftCount)
        {
            if (schematic.TradeSkillId == 0u || craftCount == 0u)
                return 0u;

            var tradeskillId = (TradeskillType)schematic.TradeSkillId;
            if (!Enum.IsDefined(tradeskillId))
                return 0u;

            uint tier = schematic.Tier + 1u;
            IEnumerable<TradeskillTierEntry> tierEntries = gameTableManager.TradeskillTier?.Entries ?? [];
            TradeskillTierEntry tierEntry = tierEntries
                .FirstOrDefault(entry => entry.TradeSkillId == schematic.TradeSkillId && entry.Tier == tier);
            if (tierEntry == null || tierEntry.CraftXp == 0u)
                return 0u;

            if (!TryMultiply(tierEntry.CraftXp, craftCount, out uint totalCraftXp))
                totalCraftXp = uint.MaxValue;

            return player.AddTradeskillXp(tradeskillId, totalCraftXp);
        }

        private static void GrantCraftingAchievements(IPlayer player, IEnumerable<GeneratedLootItem> craftedItems)
        {
            foreach (GeneratedLootItem item in craftedItems)
            {
                if (item.Type != LootItemType.StaticItem || item.StaticId == 0u || item.Count == 0u)
                    continue;

                player.AchievementManager.CheckAchievements(player, AchievementType.CraftItem, item.StaticId, 0u, item.Count);
                player.AchievementManager.CheckAchievements(player, AchievementType.CraftItemChecklist, item.StaticId);
            }
        }

        private static IEnumerable<MaterialRequirement> GetMaterialRequirements(TradeskillSchematic2Entry schematic)
        {
            if (schematic.Item2IdMaterial00 != 0u && schematic.MaterialCost00 != 0u)
                yield return new MaterialRequirement(schematic.Item2IdMaterial00, schematic.MaterialCost00);
            if (schematic.Item2IdMaterial01 != 0u && schematic.MaterialCost01 != 0u)
                yield return new MaterialRequirement(schematic.Item2IdMaterial01, schematic.MaterialCost01);
            if (schematic.Item2IdMaterial02 != 0u && schematic.MaterialCost02 != 0u)
                yield return new MaterialRequirement(schematic.Item2IdMaterial02, schematic.MaterialCost02);
            if (schematic.Item2IdMaterial03 != 0u && schematic.MaterialCost03 != 0u)
                yield return new MaterialRequirement(schematic.Item2IdMaterial03, schematic.MaterialCost03);
            if (schematic.Item2IdMaterial04 != 0u && schematic.MaterialCost04 != 0u)
                yield return new MaterialRequirement(schematic.Item2IdMaterial04, schematic.MaterialCost04);
        }

        private static bool TryBuildMaterialDebit(
            IPlayer player,
            IGameTableManager gameTableManager,
            uint item2Id,
            uint count,
            out MaterialDebit debit)
        {
            debit = default;

            IEnumerable<TradeskillMaterialEntry> materialEntries = gameTableManager.TradeskillMaterial?.Entries ?? [];
            TradeskillMaterialEntry materialEntry = materialEntries
                .SingleOrDefault(entry => entry.Item2IdStatRevolution == item2Id);

            ushort materialId = materialEntry != null && materialEntry.Id <= ushort.MaxValue ? (ushort)materialEntry.Id : (ushort)0u;
            uint satchelCount = materialId == 0u
                ? 0u
                : player.SupplySatchelManager.FirstOrDefault(material => material.MaterialId == materialId)?.Amount ?? 0u;
            uint inventoryCount = CountInventoryItems(player.Inventory, item2Id);

            if ((ulong)satchelCount + inventoryCount < count)
                return false;

            uint debitFromSatchel = Math.Min(satchelCount, count);
            debit = new MaterialDebit(item2Id, materialId, debitFromSatchel, count - debitFromSatchel);
            return true;
        }

        private static void ApplyMaterialDebit(IPlayer player, MaterialDebit debit)
        {
            if (debit.SatchelCount != 0u)
                player.SupplySatchelManager.RemoveAmount(debit.MaterialId, debit.SatchelCount);

            if (debit.InventoryCount != 0u)
                player.Inventory.ItemDelete(debit.Item2Id, debit.InventoryCount, ItemUpdateReason.Crafting);
        }

        private static bool CanCreateInventoryItem(IInventory inventory, IItemInfo info, uint count)
        {
            if (count == 0u)
                return false;

            uint remaining = count;
            IBag inventoryBag = inventory.SingleOrDefault(bag => bag.Location == InventoryLocation.Inventory);
            if (inventoryBag == null)
                return false;

            if (info.IsStackable())
            {
                foreach (IItem item in inventoryBag.Where(item => item.Info.Id == info.Id && item.ExpirationTimeLeft == 0u))
                {
                    if (item.StackCount >= info.Entry.MaxStackCount)
                        continue;

                    remaining -= Math.Min(remaining, info.Entry.MaxStackCount - item.StackCount);
                    if (remaining == 0u)
                        return true;
                }
            }

            uint perNewStack = info.IsStackable() ? info.Entry.MaxStackCount : 1u;
            if (perNewStack == 0u)
                return false;

            ulong requiredSlots = ((ulong)remaining + perNewStack - 1ul) / perNewStack;
            return requiredSlots <= inventoryBag.SlotsRemaining;
        }

        private static uint CountInventoryItems(IInventory inventory, uint item2Id)
        {
            IBag inventoryBag = inventory.SingleOrDefault(bag => bag.Location == InventoryLocation.Inventory);
            if (inventoryBag == null)
                return 0u;

            ulong total = 0ul;
            foreach (IItem item in inventoryBag.Where(item => item.Id == item2Id))
                total += item.StackCount;

            return total > uint.MaxValue ? uint.MaxValue : (uint)total;
        }

        private static bool TryMultiply(uint value, uint multiplier, out uint result)
        {
            ulong product = (ulong)value * multiplier;
            if (product > uint.MaxValue)
            {
                result = 0u;
                return false;
            }

            result = (uint)product;
            return true;
        }
    }
}
