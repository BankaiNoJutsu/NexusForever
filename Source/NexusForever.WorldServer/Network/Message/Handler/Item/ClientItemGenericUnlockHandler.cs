using NexusForever.Game.Abstract.Entity;
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
            IItem item = session.Player.Inventory.GetItem(itemGenericUnlock.Location);
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
                    itemEntry == null ? "missing-item" : "missing-generic-unlock-set");
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            GenericUnlockSetEntry unlockSetEntry = gameTableManager.GenericUnlockSet.GetEntry(itemEntry.GenericUnlockSetId);
            if (unlockSetEntry == null)
            {
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "missing-generic-unlock-set-row");
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            List<GenericUnlockEntryEntry> entries = GetGenericUnlockEntries(unlockSetEntry);
            if (entries.Count == 0)
            {
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "missing-or-invalid-generic-unlock-entries");
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.Invalid);
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
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.AlreadyUnlocked);
                return;
            }

            if (session.Player.Inventory.ItemUse(item))
                foreach (GenericUnlockEntryEntry entry in lockedEntries)
                    session.Account.GenericUnlockManager.Unlock((ushort)entry.Id);
            else
                LogInvalidUnlock(session, item, itemEntry.GenericUnlockSetId, GenericUnlockResult.Invalid, "item-consume-failed");
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
            foreach (uint genericUnlockEntryId in GetGenericUnlockEntryIds(unlockSetEntry))
            {
                if (genericUnlockEntryId == 0u)
                    continue;

                GenericUnlockEntryEntry entry = gameTableManager.GenericUnlockEntry.GetEntry(genericUnlockEntryId);
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
