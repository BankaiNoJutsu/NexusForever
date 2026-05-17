using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingSimpleCraftHandler : IMessageHandler<IWorldSession, ClientCraftingSimpleCraft>
    {
        private readonly ILogger<ClientCraftingSimpleCraftHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;

        public ClientCraftingSimpleCraftHandler(
            ILogger<ClientCraftingSimpleCraftHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
            this.itemManager      = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingSimpleCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, schematic, 1u, out string reason))
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

        public ClientCraftingComplexCraftHandler(
            ILogger<ClientCraftingComplexCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingComplexCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.PowerCoreItem2Id);

            log.LogDebug("Rejected complex craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, powerCore {PowerCoreItem2Id}, charges {ChargeCount}, reason complex-craft-state-evidence-gap.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.PowerCoreItem2Id, craft.ChargeCounts?.Length ?? 0);

            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItem>
    {
        private readonly ILogger<ClientCraftingCraftItemHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;

        public ClientCraftingCraftItemHandler(
            ILogger<ClientCraftingCraftItemHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
            this.itemManager      = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItem craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.CatalystItem2Id);

            string reason = string.Empty;
            if (craft.CatalystItem2Id == 0u &&
                CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, schematic, craft.SchematicCount, out reason))
            {
                log.LogDebug("Completed craft-item request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}.",
                    session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount);
                return;
            }

            string failureReason = craft.CatalystItem2Id != 0u ? "catalyst-craft-state-evidence-gap" : reason;
            log.LogDebug("Rejected craft-item request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}, catalyst {CatalystItem2Id}, reason {Reason}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount, craft.CatalystItem2Id, failureReason);
            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemAutoCraftHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItemAutoCraft>
    {
        private readonly ILogger<ClientCraftingCraftItemAutoCraftHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;

        public ClientCraftingCraftItemAutoCraftHandler(
            ILogger<ClientCraftingCraftItemAutoCraftHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
            this.itemManager      = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItemAutoCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);

            if (CraftingCraftRequestHelper.TryCompleteFixedRecipe(session, gameTableManager, itemManager, schematic, craft.SchematicCount, out string reason))
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
        private readonly record struct MaterialRequirement(uint Item2Id, uint Count);

        private readonly record struct MaterialDebit(uint Item2Id, ushort MaterialId, uint SatchelCount, uint InventoryCount);

        public static TradeskillSchematic2Entry GetSchematic(IGameTableManager gameTableManager, uint tradeskillSchematic2Id)
        {
            TradeskillSchematic2Entry schematic = gameTableManager.TradeskillSchematic2.GetEntry(tradeskillSchematic2Id);
            if (schematic == null)
                throw new InvalidPacketValueException();

            return schematic;
        }

        public static void ValidateItem(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            if (gameTableManager.Item.GetEntry(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public static bool TryCompleteFixedRecipe(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            TradeskillSchematic2Entry schematic,
            uint craftCount,
            out string reason)
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

            if (schematic.Item2IdOutput == 0u || schematic.OutputCount == 0u)
            {
                reason = schematic.LootId != 0u ? "loot-output-evidence-gap" : "missing-output";
                return false;
            }

            if (!TryMultiply(schematic.OutputCount, craftCount, out uint totalOutputCount))
            {
                reason = "output-count-overflow";
                return false;
            }

            IItemInfo outputInfo = itemManager.GetItemInfo(schematic.Item2IdOutput);
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

            var debits = new List<MaterialDebit>();
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

            foreach (MaterialDebit debit in debits)
                ApplyMaterialDebit(session.Player, debit);

            session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, outputInfo, totalOutputCount, ItemUpdateReason.Crafting);
            SendCraftSuccess(session, schematic, outputInfo.Id);
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

        private static void SendCraftSuccess(IWorldSession session, TradeskillSchematic2Entry schematic, uint item2IdCrafted)
        {
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = true,
                TradeskillSchematic2IdCrafted = schematic.Id,
                Item2IdCrafted = item2IdCrafted
            });
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

            TradeskillMaterialEntry materialEntry = gameTableManager.TradeskillMaterial.Entries
                .SingleOrDefault(entry => entry.Item2IdStatRevolution == item2Id);

            ushort materialId = materialEntry != null && materialEntry.Id <= ushort.MaxValue ? (ushort)materialEntry.Id : (ushort)0u;
            uint satchelCount = materialId == 0u
                ? 0u
                : player.SupplySatchelManager.FirstOrDefault(material => material.MaterialId == materialId)?.Amount ?? 0u;
            uint inventoryCount = CountInventoryItems(player.Inventory, item2Id);

            if (satchelCount + inventoryCount < count)
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
