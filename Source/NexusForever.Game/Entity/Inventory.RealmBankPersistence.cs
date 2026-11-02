using NexusForever.Game.Abstract.Entity;
using NexusForever.Database.Character;
using NexusForever.Game.Static.Entity;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private volatile Task realmBankMoveSave;
        private volatile bool realmBankSaveFailed;
        private bool BankReconciliationSaveAllowed => player is Player current && current.BankSaveAllowed && ReferenceEquals(current.BankInventory, this);
        private bool IsRealmBankPersistenceBlocked => realmBankMoveSave is { IsCompletedSuccessfully: false };
        private bool RealmBankPersistenceFailed => realmBankSaveFailed || realmBankMoveSave is { IsFaulted: true } or { IsCanceled: true };
        private bool HasPendingRealmBankItemSave => bags.Values
            .Where(bag => bag.Location == InventoryLocation.RealmBank).SelectMany(bag => bag).Any(IsBankSavePending);

        internal void RequireRealmBankReload()
        {
            lock (MutationGate) rewardRequiresReload = true;
        }

        private void EnrollPeriodicRealmBankSave(CharacterContext context)
        {
            IItem[] items = bags.Values.Where(bag => bag.Location == InventoryLocation.RealmBank).SelectMany(bag => bag).ToArray();
            if (items.Length == 0) return;
            try
            {
                if (realmBankManager == null || player == null || player.CharacterId != characterId
                    || !ReferenceEquals(player.Inventory, this))
                    throw new InvalidOperationException("Periodic bank save requires its current Character inventory owner.");
                realmBankManager.EnrollPeriodicItems(context, items, player);
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        private Task PersistRealmBankMove(IItem source, IItem destination)
        {
            if (realmBankManager == null || player == null) return null;
            if (realmBankNotifications != null)
            {
                realmBankNotifications.Stage(source); realmBankNotifications.Stage(destination); return null;
            }
            // Keep the operation, including a consumed source, owned until the
            // complete SQL unit settles. A failed unit requires a fresh inventory;
            // a later ordinary save must not persist just its Character half.
            Task save = source.Location == InventoryLocation.None
                ? realmBankManager.SaveConsumedMergeAsync(source, destination, player)
                : realmBankManager.SaveSwapAsync(source, destination, player);
            return ObserveRealmBankPersistence(save, source, destination);
        }

        private Task PersistRealmBankSplit(IItem source, Item created)
        {
            if (realmBankManager == null || player == null) return null;
            if (realmBankNotifications != null)
            {
                realmBankNotifications.Stage(source); realmBankNotifications.Stage(created, created); return null;
            }
            return ObserveRealmBankPersistence(realmBankManager.SaveSplitAsync(source, created, player), source, created);
        }

        private Task ObserveRealmBankPersistence(Task save, IItem source, IItem destination)
        {
            Task previous = realmBankMoveSave;
            if (RealmBankPersistenceFailed) realmBankSaveFailed = true;
            Task complete = previous is { IsCompletedSuccessfully: false } ? Task.WhenAll(previous, save) : save;
            realmBankMoveSave = complete;
            save.FireAndForgetAsync(error =>
            {
                realmBankSaveFailed = true;
                log.Error(error, "Failed to persist bank operation {0}/{1}; inventory requires reload.", source?.Guid, destination?.Guid);
            });
            if (!ReferenceEquals(complete, save)) complete.FireAndForgetAsync(_ => realmBankSaveFailed = true);
            return complete;
        }

        private Task PersistRealmBankDeletion(IItem item)
        {
            if (realmBankManager == null || player == null) return null;
            if (realmBankNotifications != null) { realmBankNotifications.Stage(item); return null; }
            return ObserveRealmBankPersistence(realmBankManager.SaveDeletedItemAsync(item, player), item, item);
        }
    }
}
