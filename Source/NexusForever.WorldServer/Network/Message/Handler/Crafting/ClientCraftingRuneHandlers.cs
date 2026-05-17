using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingAdditiveHandler : IMessageHandler<IWorldSession, ClientCraftingAdditive>
    {
        private readonly ILogger<ClientCraftingAdditiveHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingAdditiveHandler(
            ILogger<ClientCraftingAdditiveHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingAdditive additive)
        {
            CraftingRuneRequestHelper.ValidateItem2(gameTableManager, additive.AdditiveItem2Id);
            CraftingRuneRequestHelper.ValidateItem2(gameTableManager, additive.CatalystItem2Id);

            log.LogDebug("Rejected crafting additive request from player {PlayerGuid}: station {StationUnitId}, additive {AdditiveItem2Id}, catalyst {CatalystItem2Id}, reason active-craft-modifier-state-evidence-gap.",
                session.Player?.Guid, additive.CraftingStationUnitId, additive.AdditiveItem2Id, additive.CatalystItem2Id);
        }
    }

    public class ClientCraftingAbandonHandler : IMessageHandler<IWorldSession, ClientCraftingAbandon>
    {
        private readonly ILogger<ClientCraftingAbandonHandler> log;

        public ClientCraftingAbandonHandler(
            ILogger<ClientCraftingAbandonHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingAbandon abandon)
        {
            log.LogDebug("Rejected crafting abandon request from player {PlayerGuid}: reason active-craft-state-evidence-gap.",
                session.Player?.Guid);
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
            CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotAdd.ItemGuid);
            CraftingRuneRequestHelper.ValidateRuneType(runeSlotAdd.Type);

            log.LogDebug("Rejected rune slot add request from player {PlayerGuid}: item {ItemGuid}, isNotFusion {IsNotFusion}, type {RuneType}, reason item-rune-slot-state-evidence-gap.",
                session.Player?.Guid, runeSlotAdd.ItemGuid, runeSlotAdd.IsNotFusion, runeSlotAdd.Type);
            CraftingRuneRequestHelper.SendSigilResult(session, TradeskillResult.UnknownError);
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
            CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotClear.ItemGuid);

            log.LogDebug("Rejected rune slot clear request from player {PlayerGuid}: item {ItemGuid}, slot {RuneSlotIndex}, recover {RecoverRune}, groupCurrency {UseGroupCurrency}, reason item-rune-slot-state-evidence-gap.",
                session.Player?.Guid, runeSlotClear.ItemGuid, runeSlotClear.RuneSlotIndex, runeSlotClear.RecoverRune, runeSlotClear.UseGroupCurrency);
            CraftingRuneRequestHelper.SendSigilResult(session, TradeskillResult.UnknownError);
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
            CraftingRuneRequestHelper.GetInventoryItem(session, runeInstall.ItemGuid);

            foreach (uint item2Id in runeInstall.RuneSlotItem2Id)
                CraftingRuneRequestHelper.ValidateItem2(gameTableManager, item2Id);

            log.LogDebug("Rejected rune install request from player {PlayerGuid}: item {ItemGuid}, runeCount {RuneCount}, reason item-rune-slot-state-evidence-gap.",
                session.Player?.Guid, runeInstall.ItemGuid, runeInstall.RuneSlotItem2Id.Length);
            CraftingRuneRequestHelper.SendSigilResult(session, TradeskillResult.UnknownError);
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
            CraftingRuneRequestHelper.GetInventoryItem(session, runeSlotReroll.ItemGuid);
            CraftingRuneRequestHelper.ValidateRuneType(runeSlotReroll.Type);

            log.LogDebug("Rejected rune slot reroll request from player {PlayerGuid}: item {ItemGuid}, slot {SlotIndex}, type {RuneType}, reason item-rune-slot-state-evidence-gap.",
                session.Player?.Guid, runeSlotReroll.ItemGuid, runeSlotReroll.SlotIndex, runeSlotReroll.Type);
            CraftingRuneRequestHelper.SendSigilResult(session, TradeskillResult.UnknownError);
        }
    }

    internal static class CraftingRuneRequestHelper
    {
        public static void ValidateItem2(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            if (gameTableManager.Item.GetEntry(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public static IItem GetInventoryItem(IWorldSession session, ulong itemGuid)
        {
            IItem item = session.Player.Inventory.GetItem(itemGuid);
            if (item == null)
                throw new InvalidPacketValueException();

            return item;
        }

        public static void ValidateRuneType(RuneType type)
        {
            if (!Enum.IsDefined(type))
                throw new InvalidPacketValueException();
        }

        public static void SendSigilResult(IWorldSession session, TradeskillResult result)
        {
            session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
            {
                TradeskillSigilResult = result
            });
        }
    }
}
