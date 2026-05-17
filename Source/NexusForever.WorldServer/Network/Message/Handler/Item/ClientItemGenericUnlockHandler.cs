using NexusForever.Game.Abstract.Entity;
using System.Collections.Generic;
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

        public ClientItemGenericUnlockHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemGenericUnlock itemGenericUnlock)
        {
            IItem item = session.Player.Inventory.GetItem(itemGenericUnlock.Location);
            if (item == null)
                throw new InvalidPacketValueException();

            GenericUnlockSetEntry unlockSetEntry = gameTableManager.GenericUnlockSet.GetEntry(item.Info.Entry.GenericUnlockSetId);
            if (unlockSetEntry == null)
            {
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            List<GenericUnlockEntryEntry> entries = GetGenericUnlockEntries(unlockSetEntry);
            if (entries.Count == 0)
            {
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            List<GenericUnlockEntryEntry> lockedEntries = entries
                .Where(e => !session.Account.GenericUnlockManager.IsUnlocked(e.GenericUnlockTypeEnum, e.UnlockObject))
                .ToList();

            if (lockedEntries.Count == 0)
            {
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.AlreadyUnlocked);
                return;
            }

            if (session.Player.Inventory.ItemUse(item))
                foreach (GenericUnlockEntryEntry entry in lockedEntries)
                    session.Account.GenericUnlockManager.Unlock((ushort)entry.Id);
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
