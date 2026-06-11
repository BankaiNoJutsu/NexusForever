using System.Collections.Concurrent;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Static.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    /// <summary>
    /// Test and fallback repository that stores pending groups in memory keyed by account id.
    /// </summary>
    public sealed class InMemoryAccountPendingItemRepository : IAccountPendingItemRepository
    {
        private static readonly ConcurrentDictionary<uint, List<StoredPendingItem>> PendingByAccount = new();

        public string AppendPendingGroup(uint targetAccountId, AccountPendingItemInsert insert)
        {
            ArgumentNullException.ThrowIfNull(insert);
            ArgumentNullException.ThrowIfNull(insert.AccountItemIds);

            if (insert.AccountItemIds.Count == 0)
                throw new ArgumentException("Pending account item group must contain at least one item.", nameof(insert));

            string groupName = string.IsNullOrWhiteSpace(insert.GroupName)
                ? $"pending:{targetAccountId}:{Guid.NewGuid():N}"
                : insert.GroupName;

            NetworkIdentity senderIdentity = insert.SenderIdentity ?? new NetworkIdentity();
            NetworkIdentity targetIdentity = insert.TargetIdentity ?? new NetworkIdentity();

            List<StoredPendingItem> rows = PendingByAccount.GetOrAdd(targetAccountId, _ => []);
            lock (rows)
            {
                ulong nextId = rows.Select(p => p.PendingItemId).DefaultIfEmpty(0ul).Max() + 1ul;
                foreach (uint accountItemId in insert.AccountItemIds)
                {
                    rows.Add(new StoredPendingItem
                    {
                        PendingItemId     = nextId++,
                        GroupName         = groupName,
                        AccountItemId     = accountItemId,
                        SenderAccountId   = insert.SenderAccountId,
                        SenderRealmId     = senderIdentity.RealmId,
                        SenderCharacterId = senderIdentity.Id,
                        TargetRealmId     = targetIdentity.RealmId,
                        TargetCharacterId = targetIdentity.Id,
                        ClaimState              = insert.ClaimState,
                        HasTargetPlayerIdentity = insert.HasTargetPlayerIdentity
                    });
                }
            }

            return groupName;
        }

        public void RemovePendingGroup(uint targetAccountId, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName) || !PendingByAccount.TryGetValue(targetAccountId, out List<StoredPendingItem> rows))
                return;

            lock (rows)
                rows.RemoveAll(p => string.Equals(p.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<StoredPendingItem> GetPendingItems(uint accountId)
        {
            if (!PendingByAccount.TryGetValue(accountId, out List<StoredPendingItem> rows))
                return [];

            lock (rows)
                return rows.ToList();
        }

        public static void Clear(uint accountId)
        {
            PendingByAccount.TryRemove(accountId, out _);
        }

        public sealed class StoredPendingItem
        {
            public ulong PendingItemId { get; init; }
            public string GroupName { get; init; }
            public uint AccountItemId { get; init; }
            public uint SenderAccountId { get; init; }
            public ushort SenderRealmId { get; init; }
            public ulong SenderCharacterId { get; init; }
            public ushort TargetRealmId { get; init; }
            public ulong TargetCharacterId { get; init; }
            public AccountItemClaimState ClaimState { get; init; }
            public bool HasTargetPlayerIdentity { get; init; }
        }
    }
}
