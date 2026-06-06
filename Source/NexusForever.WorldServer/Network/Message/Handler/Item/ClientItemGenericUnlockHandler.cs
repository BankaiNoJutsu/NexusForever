using NexusForever.Game.Abstract.Entity;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.GenericUnlock;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemGenericUnlockHandler : IMessageHandler<IWorldSession, ClientItemGenericUnlock>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<ClientItemGenericUnlockHandler> log;

        public ClientItemGenericUnlockHandler(
            IGameTableManager gameTableManager,
            ILogger<ClientItemGenericUnlockHandler> log)
        {
            this.gameTableManager = gameTableManager;
            this.log              = log ?? NullLogger<ClientItemGenericUnlockHandler>.Instance;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemGenericUnlock itemGenericUnlock)
        {
            IItem item = GetItemOrDefault(session, itemGenericUnlock, out string invalidItemReason);
            Item2Entry itemEntry = item?.Info?.Entry;
            if (itemEntry == null || itemEntry.GenericUnlockSetId == 0u)
            {
                log.LogWarning("Rejected item generic unlock from player {PlayerGuid}: location={Location}, bagIndex={BagIndex}, itemGuid={ItemGuid}, item2Id={Item2Id}, genericUnlockSet={GenericUnlockSetId}, result={Result}, reason={Reason}.",
                    session.Player?.Guid,
                    itemGenericUnlock.Location.Location,
                    itemGenericUnlock.Location.BagIndex,
                    item?.Guid ?? 0ul,
                    item?.Id ?? 0u,
                    itemEntry?.GenericUnlockSetId ?? 0u,
                    GenericUnlockResult.Invalid,
                    invalidItemReason ?? (itemEntry == null ? "missing-item" : "missing-generic-unlock-set"));
                SendUnlockResult(session, GenericUnlockResult.Invalid);
                return;
            }

            GameTable<GenericUnlockSetEntry> unlockSetTable = gameTableManager?.GenericUnlockSet;
            if (unlockSetTable == null)
            {
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "missing-generic-unlock-set-table");
                SendUnlockResult(session, GenericUnlockResult.Invalid);
                return;
            }

            GenericUnlockSetEntry unlockSetEntry = unlockSetTable.GetEntry(itemEntry.GenericUnlockSetId);
            if (unlockSetEntry == null)
            {
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "missing-generic-unlock-set-row");
                SendUnlockResult(session, GenericUnlockResult.Invalid);
                return;
            }

            List<GenericUnlockEntryEntry> entries = GetGenericUnlockEntries(unlockSetEntry);
            if (entries.Count == 0)
            {
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "missing-or-invalid-generic-unlock-entries");
                SendUnlockResult(session, GenericUnlockResult.Invalid);
                return;
            }

            List<GenericUnlockEntryEntry> lockedEntries = entries
                .Where(e => !session.Account.GenericUnlockManager.IsUnlocked(e.GenericUnlockTypeEnum, e.UnlockObject))
                .ToList();

            if (lockedEntries.Count == 0)
            {
                log.LogDebug("Item generic unlock already known for player {PlayerGuid}: itemGuid={ItemGuid}, item2Id={Item2Id}, genericUnlockSet={GenericUnlockSetId}, entryIds={GenericUnlockEntryIds}, result={Result}.",
                    session.Player?.Guid,
                    item.Guid,
                    item.Id,
                    itemEntry.GenericUnlockSetId,
                    string.Join(",", entries.Select(e => e.Id)),
                    GenericUnlockResult.AlreadyUnlocked);
                SendUnlockResult(session, GenericUnlockResult.AlreadyUnlocked);
                return;
            }

            if (session.Player.Inventory.ItemUse(item))
                foreach (GenericUnlockEntryEntry entry in lockedEntries)
                    session.Account.GenericUnlockManager.Unlock((ushort)entry.Id);
            else
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "item-consume-failed");
        }

        private IItem GetItemOrDefault(IWorldSession session, ClientItemGenericUnlock itemGenericUnlock, out string invalidItemReason)
        {
            invalidItemReason = null;

            IInventory inventory = session.Player?.Inventory;
            if (inventory == null)
            {
                invalidItemReason = "missing-inventory";
                return null;
            }

            try
            {
                return inventory.GetItem(itemGenericUnlock.Location);
            }
            catch (ArgumentException)
            {
                invalidItemReason = "invalid-item-location";
                return null;
            }
        }

        private void SendUnlockResult(IWorldSession session, GenericUnlockResult result)
        {
            if (session.Account?.GenericUnlockManager == null)
            {
                log.LogWarning("Unable to send item generic unlock result for player {PlayerGuid}: result={Result}, reason={Reason}.",
                    session.Player?.Guid,
                    result,
                    "missing-generic-unlock-manager");
                return;
            }

            session.Account.GenericUnlockManager.SendUnlockResult(result);
        }

        private void LogInvalidUnlock(IWorldSession session, IItem item, uint genericUnlockSetId, GenericUnlockResult result, string reason)
        {
            log.LogWarning("Rejected item generic unlock from player {PlayerGuid}: itemGuid={ItemGuid}, item2Id={Item2Id}, genericUnlockSet={GenericUnlockSetId}, location={Location}, bagIndex={BagIndex}, stackCount={StackCount}, charges={Charges}, result={Result}, reason={Reason}.",
                session.Player?.Guid,
                item.Guid,
                item.Id,
                genericUnlockSetId,
                item.Location,
                item.BagIndex,
                item.StackCount,
                item.Charges,
                result,
                reason);
        }

        private List<GenericUnlockEntryEntry> GetGenericUnlockEntries(GenericUnlockSetEntry unlockSetEntry)
        {
            var entries = new List<GenericUnlockEntryEntry>();
            GameTable<GenericUnlockEntryEntry> unlockEntryTable = gameTableManager?.GenericUnlockEntry;
            if (unlockEntryTable == null)
                return entries;

            foreach (uint genericUnlockEntryId in GetGenericUnlockEntryIds(unlockSetEntry))
            {
                if (genericUnlockEntryId == 0u)
                    continue;

                GenericUnlockEntryEntry entry = unlockEntryTable.GetEntry(genericUnlockEntryId);
                if (entry == null || entry.Id > ushort.MaxValue)
                    return [];

                entries.Add(entry);
            }

            return entries;
        }

        private static IEnumerable<uint> GetGenericUnlockEntryIds(GenericUnlockSetEntry entry)
        {
            yield return entry.GenericUnlockEntryId00;
            yield return entry.GenericUnlockEntryId01;
            yield return entry.GenericUnlockEntryId02;
            yield return entry.GenericUnlockEntryId03;
            yield return entry.GenericUnlockEntryId04;
            yield return entry.GenericUnlockEntryId05;
        }
    }
}
