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

        public void AppendPendingGroup(uint targetAccountId, AccountPendingItemInsert insert)
        {
            ArgumentNullException.ThrowIfNull(insert);

            string groupName = string.IsNullOrWhiteSpace(insert.GroupName)
                ? $"pending:{targetAccountId}:{Guid.NewGuid():N}"
                : insert.GroupName;

            ulong nextId = PendingByAccount.GetOrAdd(targetAccountId, _ => []).Select(p => p.PendingItemId).DefaultIfEmpty(0ul).Max() + 1ul;
            NetworkIdentity senderIdentity = insert.SenderIdentity ?? new NetworkIdentity();
            NetworkIdentity targetIdentity = insert.TargetIdentity ?? new NetworkIdentity();

            List<StoredPendingItem> rows = PendingByAccount.GetOrAdd(targetAccountId, _ => []);
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
                    ClaimState        = insert.ClaimState,
                    Unknown1          = insert.Unknown1
                });
            }
        }

        public static IReadOnlyList<StoredPendingItem> GetPendingItems(uint accountId)
        {
            return PendingByAccount.TryGetValue(accountId, out List<StoredPendingItem> rows)
                ? rows.ToList()
                : [];
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
            public bool Unknown1 { get; init; }
        }
    }
}
