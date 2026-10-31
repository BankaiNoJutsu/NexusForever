using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public partial class AccountInventoryManager
    {
        private readonly object pendingGate = new();
        private Dictionary<PendingAccountItem, PendingState> pendingBaseline = [];
        private bool pendingSaveInFlight;

        private static PendingState CapturePendingState(PendingAccountItem item) => new(
            item.Id, item.Group, item.AccountItemId, item.SenderAccountId,
            item.SenderIdentity?.RealmId ?? 0, item.SenderIdentity?.Id ?? 0ul,
            item.TargetIdentity?.RealmId ?? 0, item.TargetIdentity?.Id ?? 0ul,
            (byte)item.ClaimState, item.HasTargetPlayerIdentity, item.ForceSoulbind);

        private void SavePendingItems(AuthContext context)
        {
            lock (pendingGate)
            {
                if (pendingSaveInFlight)
                    return;

                Dictionary<PendingAccountItem, PendingState> snapshot = pendingGroups.Values.SelectMany(g => g)
                    .ToDictionary(item => item, CapturePendingState);
                Dictionary<PendingAccountItem, PendingState> baseline = new(pendingBaseline);
                if (snapshot.Count == baseline.Count && snapshot.All(pair => baseline.TryGetValue(pair.Key, out PendingState state) && state == pair.Value))
                    return;

                var staged = new List<object>();
                var allocated = new Dictionary<PendingAccountItem, ulong>();
                var consumed = new HashSet<PendingAccountItem>();
                pendingSaveInFlight = true;
                try
                {
                    context.EnlistSaveWork(unit =>
                    {
                        // A provider retry replaces only this owner's previous staging.
                        foreach (object row in staged)
                            unit.Entry(row).State = EntityState.Detached;
                        staged.Clear();
                        allocated.Clear();
                        consumed.Clear();

                        unit.ReservePendingItemIds(account.Id, 0);
                        Dictionary<ulong, AccountPendingItemModel> persisted = unit.LoadPendingItemRows(account.Id).ToDictionary(row => row.PendingItemId);

                        foreach ((PendingAccountItem item, PendingState before) in baseline)
                        {
                            if (!persisted.TryGetValue(before.Id, out AccountPendingItemModel row))
                            {
                                consumed.Add(item);
                                continue;
                            }
                            if (!snapshot.TryGetValue(item, out PendingState after))
                            {
                                unit.AccountPendingItem.Remove(row);
                                staged.Add(row);
                            }
                            else if (after != before)
                            {
                                EntityEntry<AccountPendingItemModel> entry = unit.Attach(row);
                                ApplyChangedPendingFields(entry, before, after);
                                staged.Add(row);
                            }
                        }

                        foreach ((PendingAccountItem item, PendingState state) in snapshot)
                        {
                            if (baseline.ContainsKey(item))
                                continue;
                            var row = new AccountPendingItemModel
                            {
                                Id = account.Id, GroupName = state.Group,
                                AccountItemId = state.AccountItemId, SenderAccountId = state.SenderAccountId,
                                SenderRealmId = state.SenderRealmId, SenderCharacterId = state.SenderCharacterId,
                                TargetRealmId = state.TargetRealmId, TargetCharacterId = state.TargetCharacterId,
                                ClaimState = state.ClaimState, HasTargetPlayerIdentity = state.HasTargetPlayerIdentity, ForceSoulbind = state.ForceSoulbind
                            };
                            (ulong id, bool available) = unit.StagePendingItemAppend(item.DeliveryKey, row);
                            if (unit.Entry(row).State == EntityState.Added)
                            {
                                staged.Add(row);
                                staged.Add(unit.AccountPendingItemDelivery.Local.Single(receipt => receipt.AccountId == account.Id && receipt.DeliveryKey == item.DeliveryKey));
                            }
                            allocated.Add(item, id);
                            if (!available)
                                consumed.Add(item);
                        }
                    }, committed =>
                    {
                        lock (pendingGate)
                        {
                            pendingSaveInFlight = false;
                            foreach (object row in staged)
                            {
                                try { context.Entry(row).State = EntityState.Detached; }
                                catch (ObjectDisposedException) { /* Context disposal already discarded this staging. */ }
                            }
                            if (committed)
                            {
                                var reconciledGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                foreach ((PendingAccountItem item, ulong id) in allocated)
                                {
                                    if (item.Id != id && item.Notified && pendingGroups.TryGetValue(item.Group, out List<PendingAccountItem> current) && current.Contains(item))
                                        reconciledGroups.Add(item.Group);
                                    item.Id = id;
                                    nextPendingItemId = Math.Max(nextPendingItemId, checked(id + 1ul));
                                    snapshot[item] = snapshot[item] with { Id = id };
                                }
                                foreach (PendingAccountItem item in consumed)
                                {
                                    snapshot.Remove(item);
                                    if (pendingGroups.TryGetValue(item.Group, out List<PendingAccountItem> group))
                                    {
                                        group.Remove(item);
                                        if (item.Notified)
                                            reconciledGroups.Add(item.Group);
                                        if (group.Count == 0)
                                            pendingGroups.Remove(item.Group);
                                    }
                                }
                                // A new row may have received a provisional ID while
                                // this unit was staged. Committed IDs take precedence;
                                // keep every remaining live cache identity distinct.
                                var usedIds = new HashSet<ulong>(allocated.Values);
                                foreach (PendingAccountItem item in pendingGroups.Values.SelectMany(group => group))
                                {
                                    if (allocated.ContainsKey(item) || usedIds.Add(item.Id))
                                        continue;
                                    if (item.Notified)
                                        reconciledGroups.Add(item.Group);
                                    do { item.Id = GetNextPendingItemId(); } while (!usedIds.Add(item.Id));
                                }
                                // A captured item removed or edited while saving remains a delta
                                // against this committed snapshot on the next save.
                                // Another owner's completion may have adopted newly
                                // committed deliveries after this snapshot was captured.
                                // Acknowledge only this unit's owned objects.
                                var merged = pendingBaseline.Where(pair => !baseline.ContainsKey(pair.Key) && !snapshot.ContainsKey(pair.Key))
                                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                                foreach (var pair in snapshot) merged[pair.Key] = pair.Value;
                                pendingBaseline = merged;
                                // IDs are client cache keys even though requests use group
                                // names. Replace the affected group when allocation rebases
                                // a previously announced provisional ID.
                                foreach (string group in reconciledGroups)
                                    account.Session.EnqueueMessageEncrypted(new ServerAccountPendingItemGroupDelete { Value = group, Flag = true });
                                foreach (string group in reconciledGroups)
                                    SendPendingGroupAdded(group);
                            }
                        }
                    });
                }
                catch
                {
                    pendingSaveInFlight = false;
                    throw;
                }
            }
        }

        private static void ApplyChangedPendingFields(EntityEntry<AccountPendingItemModel> entry, PendingState before, PendingState after)
        {
            if (before.Group != after.Group) entry.Property(row => row.GroupName).CurrentValue = after.Group;
            if (before.AccountItemId != after.AccountItemId) entry.Property(row => row.AccountItemId).CurrentValue = after.AccountItemId;
            if (before.SenderAccountId != after.SenderAccountId) entry.Property(row => row.SenderAccountId).CurrentValue = after.SenderAccountId;
            if (before.SenderRealmId != after.SenderRealmId) entry.Property(row => row.SenderRealmId).CurrentValue = after.SenderRealmId;
            if (before.SenderCharacterId != after.SenderCharacterId) entry.Property(row => row.SenderCharacterId).CurrentValue = after.SenderCharacterId;
            if (before.TargetRealmId != after.TargetRealmId) entry.Property(row => row.TargetRealmId).CurrentValue = after.TargetRealmId;
            if (before.TargetCharacterId != after.TargetCharacterId) entry.Property(row => row.TargetCharacterId).CurrentValue = after.TargetCharacterId;
            if (before.ClaimState != after.ClaimState) entry.Property(row => row.ClaimState).CurrentValue = after.ClaimState;
            if (before.HasTargetPlayerIdentity != after.HasTargetPlayerIdentity) entry.Property(row => row.HasTargetPlayerIdentity).CurrentValue = after.HasTargetPlayerIdentity;
            if (before.ForceSoulbind != after.ForceSoulbind) entry.Property(row => row.ForceSoulbind).CurrentValue = after.ForceSoulbind;
        }

        private sealed record PendingState(ulong Id, string Group, uint AccountItemId, uint SenderAccountId,
            ushort SenderRealmId, ulong SenderCharacterId, ushort TargetRealmId, ulong TargetCharacterId,
            byte ClaimState, bool HasTargetPlayerIdentity, bool ForceSoulbind);
    }
}
