using NexusForever.Database.Auth.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Network.World.Message.Model;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public partial class AccountInventoryManager
    {
        public bool SupportsRewardBinding => true;

        public string AddPendingItemGroupWithBinding(IEnumerable<uint> accountItemIds, IReadOnlyList<bool> forceSoulbind,
            NetworkIdentity senderIdentity = null, NetworkIdentity targetPlayerIdentity = null, string group = null,
            bool notify = true, uint senderAccountId = 0u, ulong targetAccountId = 0ul)
        {
            ArgumentNullException.ThrowIfNull(accountItemIds);
            List<uint> ids = accountItemIds.ToList();
            if (forceSoulbind == null || forceSoulbind.Count != ids.Count)
                throw new ArgumentException("Binding metadata must match every pending item.", nameof(forceSoulbind));
            lock (pendingGate)
            {
                string name = AddPendingItemGroupCore(ids, senderIdentity, targetPlayerIdentity, group, false, senderAccountId, targetAccountId);
                for (int i = 0; i < ids.Count; i++) pendingGroups[name][i].ForceSoulbind = forceSoulbind[i];
                if (notify) SendPendingGroupAdded(name);
                return name;
            }
        }

        internal void AdoptCommittedPendingItems(IReadOnlyList<AccountPendingItemModel> rows, Func<bool> owns = null)
        {
            var session = account.Session;
            if (owns?.Invoke() == false) return;
            lock (pendingGate)
            {
                var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var durableIds = rows.Select(row => row.PendingItemId).ToHashSet();
                foreach (PendingAccountItem known in pendingBaseline.Keys.ToArray())
                    if (!durableIds.Contains(known.Id))
                    {
                        if (pendingGroups.TryGetValue(known.Group, out List<PendingAccountItem> group) && group.Remove(known))
                        {
                            if (known.Notified) affected.Add(known.Group);
                            if (group.Count == 0) pendingGroups.Remove(known.Group);
                        }
                        pendingBaseline.Remove(known);
                    }

                var baselineIds = pendingBaseline.Keys.Select(item => item.Id).ToHashSet();
                var occupied = new HashSet<ulong>(durableIds);
                foreach (PendingAccountItem local in pendingGroups.Values.SelectMany(group => group))
                {
                    if (pendingBaseline.ContainsKey(local)) continue;
                    if (occupied.Add(local.Id)) continue;
                    if (local.Notified) affected.Add(local.Group);
                    do { local.Id = GetNextPendingItemId(); } while (!occupied.Add(local.Id));
                }
                foreach (AccountPendingItemModel row in rows)
                {
                    if (row.Id != account.Id || baselineIds.Contains(row.PendingItemId)) continue;
                    var item = new PendingAccountItem
                    {
                        Id = row.PendingItemId, AccountItemId = row.AccountItemId, Group = row.GroupName,
                        SenderAccountId = row.SenderAccountId,
                        TargetAccountId = row.SenderAccountId != 0 && row.TargetCharacterId == 0 ? row.Id : 0,
                        SenderIdentity = new NetworkIdentity { RealmId = row.SenderRealmId, Id = row.SenderCharacterId },
                        TargetIdentity = new NetworkIdentity { RealmId = row.TargetRealmId, Id = row.TargetCharacterId },
                        ClaimState = (AccountItemClaimState)row.ClaimState, HasTargetPlayerIdentity = row.HasTargetPlayerIdentity,
                        ForceSoulbind = row.ForceSoulbind, Notified = false
                    };
                    if (!pendingGroups.TryGetValue(item.Group, out List<PendingAccountItem> group)) pendingGroups[item.Group] = group = [];
                    group.Add(item);
                    pendingBaseline.Add(item, CapturePendingState(item));
                    nextPendingItemId = Math.Max(nextPendingItemId, checked(item.Id + 1));
                    affected.Add(item.Group);
                }
                foreach (string group in affected)
                {
                    if (owns?.Invoke() == false) return;
                    session.EnqueueMessageEncrypted(new ServerAccountPendingItemGroupDelete { Value = group, Flag = true });
                }
                foreach (string group in affected) SendPendingGroupAdded(group, session, owns);
            }
        }
    }
}
