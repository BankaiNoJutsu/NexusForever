using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Static;

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

            bool applied = CraftingRuneRequestHelper.ApplyCraftingAdditive(session, additive.AdditiveItem2Id, additive.CatalystItem2Id);
            log.LogDebug("Processed crafting additive request from player {PlayerGuid}: station {StationUnitId}, additive {AdditiveItem2Id}, catalyst {CatalystItem2Id}, applied {Applied}.",
                session.Player?.Guid, additive.CraftingStationUnitId, additive.AdditiveItem2Id, additive.CatalystItem2Id, applied);
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
            CraftingRuneRequestHelper.ClearCraftingAdditives(session);
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
            CraftingRuneRequestHelper.ValidateRuneType(runeSlotAdd.Type);

            TradeskillResult result = CraftingRuneRequestHelper.AddRuneSlot(item, runeSlotAdd.Type);
            log.LogDebug("Processed rune slot add request from player {PlayerGuid}: item {ItemGuid}, isNotFusion {IsNotFusion}, type {RuneType}, result {Result}.",
                session.Player?.Guid, runeSlotAdd.ItemGuid, runeSlotAdd.IsNotFusion, runeSlotAdd.Type, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
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

            TradeskillResult result = CraftingRuneRequestHelper.InstallRunes(session, item, runeInstall.RuneSlotItem2Id);
            log.LogDebug("Processed rune install request from player {PlayerGuid}: item {ItemGuid}, runeCount {RuneCount}, result {Result}.",
                session.Player?.Guid, runeInstall.ItemGuid, runeInstall.RuneSlotItem2Id.Length, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
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
            CraftingRuneRequestHelper.ValidateRuneType(runeSlotReroll.Type);

            TradeskillResult result = CraftingRuneRequestHelper.RerollRuneSlot(item, runeSlotReroll.SlotIndex, runeSlotReroll.Type);
            log.LogDebug("Processed rune slot reroll request from player {PlayerGuid}: item {ItemGuid}, slot {SlotIndex}, type {RuneType}, result {Result}.",
                session.Player?.Guid, runeSlotReroll.ItemGuid, runeSlotReroll.SlotIndex, runeSlotReroll.Type, result);
            CraftingRuneRequestHelper.SendSigilResult(session, result);
        }
    }

    internal static class CraftingRuneRequestHelper
    {
        private const int MaxRuneSlots = 8;
        private static readonly object syncRoot = new();
        private static readonly Dictionary<ulong, List<RuneSlotState>> runeSlotsByItemGuid = [];
        private static readonly Dictionary<ulong, CraftingModifierState> activeCraftingModifiersByCharacterId = [];

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

        public static bool ApplyCraftingAdditive(IWorldSession session, uint additiveItem2Id, uint catalystItem2Id)
        {
            if (session.Player == null)
                return false;

            lock (syncRoot)
            {
                activeCraftingModifiersByCharacterId[session.Player.CharacterId] = new CraftingModifierState(additiveItem2Id, catalystItem2Id);
            }

            return true;
        }

        public static void ClearCraftingAdditives(IWorldSession session)
        {
            if (session.Player == null)
                return;

            lock (syncRoot)
                activeCraftingModifiersByCharacterId.Remove(session.Player.CharacterId);
        }

        public static TradeskillResult AddRuneSlot(IItem item, RuneType type)
        {
            lock (syncRoot)
            {
                List<RuneSlotState> slots = GetRuneSlots(item.Guid);
                if (slots.Count >= MaxRuneSlots)
                    return TradeskillResult.RuneSlotLimit;

                slots.Add(new RuneSlotState(type));
                return TradeskillResult.Success;
            }
        }

        public static TradeskillResult ClearRuneSlot(IWorldSession session, IItem item, uint slotIndex, bool recoverRune)
        {
            lock (syncRoot)
            {
                List<RuneSlotState> slots = GetRuneSlots(item.Guid);
                if (slotIndex >= slots.Count)
                    return TradeskillResult.InvalidSlot;

                RuneSlotState slot = slots[(int)slotIndex];
                if (recoverRune && slot.RuneItem2Id != 0u)
                    session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, slot.RuneItem2Id, 1u, ItemUpdateReason.TradeskillGlyph);

                slot.RuneItem2Id = 0u;
                return TradeskillResult.Success;
            }
        }

        public static TradeskillResult InstallRunes(IWorldSession session, IItem item, IReadOnlyList<uint> runeItem2Ids)
        {
            if (session.Player == null)
                return TradeskillResult.UnknownError;

            lock (syncRoot)
            {
                List<RuneSlotState> slots = GetRuneSlots(item.Guid);
                if (runeItem2Ids.Count > slots.Count)
                    return TradeskillResult.InvalidSlot;

                foreach (uint runeItem2Id in runeItem2Ids.Where(id => id != 0u))
                {
                    if (!session.Player.Inventory.HasItemCount(runeItem2Id, 1u))
                        return TradeskillResult.MissingRune;
                }

                foreach (uint runeItem2Id in runeItem2Ids.Where(id => id != 0u))
                    session.Player.Inventory.ItemDelete(runeItem2Id, 1u, ItemUpdateReason.TradeskillGlyph);

                for (int i = 0; i < runeItem2Ids.Count; i++)
                    slots[i].RuneItem2Id = runeItem2Ids[i];

                return TradeskillResult.Success;
            }
        }

        public static TradeskillResult RerollRuneSlot(IItem item, uint slotIndex, RuneType type)
        {
            lock (syncRoot)
            {
                List<RuneSlotState> slots = GetRuneSlots(item.Guid);
                if (slotIndex >= slots.Count)
                    return TradeskillResult.InvalidSlot;

                slots[(int)slotIndex].Type = type;
                return TradeskillResult.Success;
            }
        }

        private static List<RuneSlotState> GetRuneSlots(ulong itemGuid)
        {
            if (!runeSlotsByItemGuid.TryGetValue(itemGuid, out List<RuneSlotState> slots))
            {
                slots = [];
                runeSlotsByItemGuid.Add(itemGuid, slots);
            }

            return slots;
        }

        private sealed record CraftingModifierState(uint AdditiveItem2Id, uint CatalystItem2Id);

        private sealed class RuneSlotState
        {
            public RuneType Type { get; set; }
            public uint RuneItem2Id { get; set; }

            public RuneSlotState(RuneType type)
            {
                Type = type;
            }
        }
    }
}
