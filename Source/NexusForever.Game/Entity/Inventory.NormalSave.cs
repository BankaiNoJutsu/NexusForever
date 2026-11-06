using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal Action<CharacterContext> CaptureNormalSave(string database, bool includePathRewards = false)
        {
            lock (MutationGate)
            {
                RequireNormalSaveAllowed();
                PathItemCapture[] pathItems = includePathRewards ? pathItemPublication?.Capture() ?? [] : [];
                IItem[] tombstones = deletedItems.ToArray();
                IItem[] captured = bags.Values.Where(bag => bag.Location != InventoryLocation.Ability && bag.Location != InventoryLocation.RealmBank)
                    .SelectMany(bag => bag).Concat(tombstones).Distinct().ToArray();
                var stages = captured.Select(item =>
                {
                    // This existing snapshot already supports Character storage
                    // and consumes only captured Item field revisions. Reuse that
                    // route without replacing later runtime values or membership.
                    if (item is Item concrete)
                    {
                        var snapshot = new Item.BankSaveSnapshot(concrete);
                        return (Action<CharacterContext>)snapshot.StageNormal;
                    }
                    return item.Save;
                }).ToArray();
                return context =>
                {
                    lock (MutationGate)
                    {
                        try
                        {
                            CharacterDatabaseIdentity.Require(context, database); RequireNormalSaveAllowed();
                            using var mutation = new InventoryMutationOperation(this);
                            foreach (var stage in stages) stage(context);
                            StagePathItemNotices(context, pathItems);
                            context.OnSaveCompleted(committed =>
                            {
                                if (!committed) return;
                                lock (MutationGate)
                                    foreach (IItem item in tombstones)
                                        if (item is Item concrete ? !concrete.HasUnsettledDeletion : !item.PendingDelete)
                                            deletedItems.RemoveAll(deleted => ReferenceEquals(deleted, item));
                            });
                            EnrollPeriodicRealmBankSave(context);
                        }
                        catch (Exception error) { context.RejectSave(error); throw; }
                    }
                };
            }
        }
        private void RequireNormalSaveAllowed()
        {
            if (realmBankPublishing || RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked && !BankReconciliationSaveAllowed || realmBankNotifications != null || HasPendingRewardDelivery)
                throw new InvalidOperationException("Ordinary inventory save overlaps a pending reward delivery.");
        }
    }
}
