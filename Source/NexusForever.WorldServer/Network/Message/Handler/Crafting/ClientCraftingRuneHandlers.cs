using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingAdditiveHandler : IMessageHandler<IWorldSession, ClientCraftingAdditive>
    {
        private readonly ILogger<ClientCraftingAdditiveHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingAdditiveHandler(
            ILogger<ClientCraftingAdditiveHandler> log,
            IGameTableManager gameTableManager,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.gameTableManager             = gameTableManager;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingAdditive additive)
        {
            CraftingRuneRequestHelper.ValidateCraftingAdditive(gameTableManager, additive.AdditiveItem2Id);
            CraftingRuneRequestHelper.ValidateCraftingCatalyst(gameTableManager, additive.CatalystItem2Id);

            if (!CraftingCraftRequestHelper.TryValidateAnyCraftingStation(session.Player, additive.CraftingStationUnitId, out string reason))
            {
                log.LogDebug("Rejected crafting additive request from player {PlayerGuid}: station {StationUnitId}, additive {AdditiveItem2Id}, catalyst {CatalystItem2Id}, reason {Reason}.",
                    session.Player?.Guid, additive.CraftingStationUnitId, additive.AdditiveItem2Id, additive.CatalystItem2Id, reason);
                throw new InvalidPacketValueException();
            }

            bool applied = craftingModifierSessionStore.TryAddModifier(session.Player, additive.AdditiveItem2Id, additive.CatalystItem2Id);
            if (!applied)
                session.Player?.SendGenericError(GenericError.CraftTooManyAdditives);

            log.LogDebug("Processed crafting additive request from player {PlayerGuid}: station {StationUnitId}, additive {AdditiveItem2Id}, catalyst {CatalystItem2Id}, applied {Applied}.",
                session.Player?.Guid, additive.CraftingStationUnitId, additive.AdditiveItem2Id, additive.CatalystItem2Id, applied);
        }
    }

    public class ClientCraftingAbandonHandler : IMessageHandler<IWorldSession, ClientCraftingAbandon>
    {
        private readonly ILogger<ClientCraftingAbandonHandler> log;
        private readonly ICraftingModifierSessionStore craftingModifierSessionStore;

        public ClientCraftingAbandonHandler(
            ILogger<ClientCraftingAbandonHandler> log,
            ICraftingModifierSessionStore craftingModifierSessionStore)
        {
            this.log                          = log;
            this.craftingModifierSessionStore = craftingModifierSessionStore;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingAbandon abandon)
        {
            craftingModifierSessionStore.ClearModifiers(session.Player);
            log.LogDebug("Processed crafting abandon request from player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientCraftingRuneSlotAddHandler : IMessageHandler<IWorldSession, ClientCraftingRuneSlotAdd>
    {
        private readonly ILogger<ClientCraftingRuneSlotAddHandler> log;

        public ClientCraftingRuneSlotAddHandler(
            ILogger<ClientCraftingRuneSlotAddHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingRuneSlotAdd runeSlotAdd)
        {
            IItem item = CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotAdd.ItemGuid);
            RuneType type = CraftingRuneRequestHelper.NormalizeRuneType(runeSlotAdd.Type);

            TradeskillResult result = CraftingRuneRequestHelper.AddRuneSlot(item, type);
            log.LogDebug("Processed rune slot add request from player {PlayerGuid}: item {ItemGuid}, isNotFusion {IsNotFusion}, rawType {RawRuneType}, type {RuneType}, result {Result}.",
                session.Player?.Guid, runeSlotAdd.ItemGuid, runeSlotAdd.IsNotFusion, runeSlotAdd.Type, type, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
            if (result == TradeskillResult.Success)
                CraftingRuneRequestHelper.SendItemRefresh(session, item);
        }
    }

    public class ClientCraftingRuneSlotClearHandler : IMessageHandler<IWorldSession, ClientCraftingRuneSlotClear>
    {
        private readonly ILogger<ClientCraftingRuneSlotClearHandler> log;

        public ClientCraftingRuneSlotClearHandler(
            ILogger<ClientCraftingRuneSlotClearHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingRuneSlotClear runeSlotClear)
        {
            IItem item = CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotClear.ItemGuid);

            TradeskillResult result = CraftingRuneRequestHelper.ClearRuneSlot(session, item, runeSlotClear.RuneSlotIndex, runeSlotClear.RecoverRune);
            log.LogDebug("Processed rune slot clear request from player {PlayerGuid}: item {ItemGuid}, slot {RuneSlotIndex}, recover {RecoverRune}, groupCurrency {UseGroupCurrency}, result {Result}.",
                session.Player?.Guid, runeSlotClear.ItemGuid, runeSlotClear.RuneSlotIndex, runeSlotClear.RecoverRune, runeSlotClear.UseGroupCurrency, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
            if (result == TradeskillResult.Success)
                CraftingRuneRequestHelper.SendItemRefresh(session, item);
        }
    }

    public class ClientCraftingRuneInstallHandler : IMessageHandler<IWorldSession, ClientCraftingRuneInstall>
    {
        private readonly ILogger<ClientCraftingRuneInstallHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingRuneInstallHandler(
            ILogger<ClientCraftingRuneInstallHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingRuneInstall runeInstall)
        {
            IItem item = CraftingRuneRequestHelper.GetInventoryItem(session, runeInstall.ItemGuid);

            foreach (uint item2Id in runeInstall.RuneSlotItem2Id)
                CraftingRuneRequestHelper.ValidateItem2(gameTableManager, item2Id);

            TradeskillResult result = CraftingRuneRequestHelper.InstallRunes(gameTableManager, session, item, runeInstall.RuneSlotItem2Id);
            log.LogDebug("Processed rune install request from player {PlayerGuid}: item {ItemGuid}, runeCount {RuneCount}, result {Result}.",
                session.Player?.Guid, runeInstall.ItemGuid, runeInstall.RuneSlotItem2Id.Length, result);
            CraftingRuneRequestHelper.SendInstallFailure(session, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
            if (result == TradeskillResult.Success)
                CraftingRuneRequestHelper.SendItemRefresh(session, item);
        }
    }

    public class ClientCraftingRuneSlotRerollHandler : IMessageHandler<IWorldSession, ClientCraftingRuneSlotReroll>
    {
        private readonly ILogger<ClientCraftingRuneSlotRerollHandler> log;

        public ClientCraftingRuneSlotRerollHandler(
            ILogger<ClientCraftingRuneSlotRerollHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingRuneSlotReroll runeSlotReroll)
        {
            IItem item = CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotReroll.ItemGuid);
            RuneType type = CraftingRuneRequestHelper.NormalizeRuneType(runeSlotReroll.Type, compactFirst: true);

            TradeskillResult result = CraftingRuneRequestHelper.RerollRuneSlot(item, runeSlotReroll.SlotIndex, type);
            log.LogDebug("Processed rune slot reroll request from player {PlayerGuid}: item {ItemGuid}, slot {SlotIndex}, rawType {RawRuneType}, type {RuneType}, result {Result}.",
                session.Player?.Guid, runeSlotReroll.ItemGuid, runeSlotReroll.SlotIndex, runeSlotReroll.Type, type, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
            if (result == TradeskillResult.Success)
                CraftingRuneRequestHelper.SendItemRefresh(session, item);
        }
    }

    internal static class CraftingRuneRequestHelper
    {
        private const int MaxRuneSlots = 8;

        public static void ValidateItem2(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            if (gameTableManager.Item.GetEntry(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public static void ValidateCraftingAdditive(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            Item2Entry item = gameTableManager.Item.GetEntry(item2Id);
            if (item == null || item.TradeskillAdditiveId == 0u || gameTableManager.TradeskillAdditive.GetEntry(item.TradeskillAdditiveId) == null)
                throw new InvalidPacketValueException();
        }

        public static void ValidateCraftingCatalyst(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            Item2Entry item = gameTableManager.Item.GetEntry(item2Id);
            if (item == null || item.TradeskillCatalystId == 0u || gameTableManager.TradeskillCatalyst.GetEntry(item.TradeskillCatalystId) == null)
                throw new InvalidPacketValueException();
        }

        public static IItem GetInventoryItem(IWorldSession session, ulong itemGuid)
        {
            IItem item = session.Player.Inventory.GetItem(itemGuid);
            if (item == null)
                throw new InvalidPacketValueException();

            return item;
        }

        public static RuneType NormalizeRuneType(RuneType type, bool compactFirst = false)
        {
            uint value = (uint)type;
            if (compactFirst && value >= 1u && value <= 7u)
                return (RuneType)(value + 6u);

            if (ItemRuneSocketTypes.TryToRuneType(value, out RuneType normalized))
                return normalized;

            throw new InvalidPacketValueException();
        }

        public static void SendSigilResult(IWorldSession session, TradeskillResult result)
        {
            session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
            {
                TradeskillSigilResult = result
            });
        }

        /// <summary>
        /// Mirrors client <c>RuneCrafting_SendClientRuneInstall</c> (<c>14059d250</c>) emitting
        /// <see cref="GenericError.CraftMicrochipInvalidSocket"/> when pre-send validation fails.
        /// </summary>
        public static void SendInstallFailure(IWorldSession session, TradeskillResult result)
        {
            if (result == TradeskillResult.Success || session.Player == null)
                return;

            switch (result)
            {
                case TradeskillResult.InvalidSlot:
                    session.Player.SendGenericError(GenericError.CraftMicrochipInvalidSocket);
                    break;
                case TradeskillResult.MissingRune:
                    session.Player.SendGenericError(GenericError.ItemBadId);
                    break;
            }
        }

        public static void SendItemRefresh(IWorldSession session, IItem item)
        {
            if (session.Player?.IsLoading ?? true)
                return;

            session.EnqueueMessageEncrypted(new ServerItemAdd
            {
                InventoryItem = new InventoryItem
                {
                    Item   = item.Build(),
                    Reason = ItemUpdateReason.TradeskillGlyph
                }
            });
        }

        public static TradeskillResult AddRuneSlot(IItem item, RuneType type)
        {
            if (item.RuneSlots.Count >= MaxRuneSlots)
                return TradeskillResult.RuneSlotLimit;

            item.RuneSlots.Add(new ItemRuneSlot(type));
            item.TouchRuneSlots();
            return TradeskillResult.Success;
        }

        public static TradeskillResult ClearRuneSlot(IWorldSession session, IItem item, uint slotIndex, bool recoverRune)
        {
            if (slotIndex >= item.RuneSlots.Count)
                return TradeskillResult.InvalidSlot;

            ItemRuneSlot slot = item.RuneSlots[(int)slotIndex];
            if (recoverRune && slot.RuneItem2Id != 0u)
                session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, slot.RuneItem2Id, 1u, ItemUpdateReason.TradeskillGlyph);

            slot.RuneItem2Id = 0u;
            item.TouchRuneSlots();
            return TradeskillResult.Success;
        }

        public static TradeskillResult InstallRunes(IGameTableManager gameTableManager, IWorldSession session, IItem item, IReadOnlyList<uint> runeItem2Ids)
        {
            if (session.Player == null)
                return TradeskillResult.UnknownError;

            TradeskillResult layoutResult = ItemRuneInstallValidator.ValidateInstallTargets(gameTableManager, item, runeItem2Ids);
            if (layoutResult != TradeskillResult.Success)
                return layoutResult;

            foreach (uint runeItem2Id in runeItem2Ids.Where(id => id != 0u))
            {
                if (!session.Player.Inventory.HasItemCount(runeItem2Id, 1u))
                    return TradeskillResult.MissingRune;
            }

            foreach (uint runeItem2Id in runeItem2Ids.Where(id => id != 0u))
                session.Player.Inventory.ItemDelete(runeItem2Id, 1u, ItemUpdateReason.TradeskillGlyph);

            for (int i = 0; i < runeItem2Ids.Count; i++)
                item.RuneSlots[i].RuneItem2Id = runeItem2Ids[i];

            item.TouchRuneSlots();
            return TradeskillResult.Success;
        }

        public static TradeskillResult RerollRuneSlot(IItem item, uint slotIndex, RuneType type)
        {
            if (slotIndex >= item.RuneSlots.Count)
                return TradeskillResult.InvalidSlot;

            item.RuneSlots[(int)slotIndex].Type = type;
            item.TouchRuneSlots();
            return TradeskillResult.Success;
        }
    }
}
