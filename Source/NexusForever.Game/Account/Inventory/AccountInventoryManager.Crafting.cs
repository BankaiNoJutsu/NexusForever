using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Session;

namespace NexusForever.Game.Account.Inventory
{
    public partial class AccountInventoryManager
    {
        internal IReadOnlyList<IAccountInventoryItem> AddLootItems(uint accountItemId, uint count)
        {
            if (count == 0) return [];
            if (!CanAddItem(accountItemId)) throw new ArgumentException("Unknown account loot item.", nameof(accountItemId));
            // Reserve the complete range before adopting or announcing any item.
            // An allocator I/O failure leaves the whole loot entry retryable.
            ulong first = GetNextInventoryIds(count);
            var added = new List<IAccountInventoryItem>();
            for (uint index = 0; index < count; index++)
                added.Add(new AccountInventoryItem(account, checked(first + index), accountItemId, null,
                    AccountItemClaimState.CanClaim, false, gameTableManager, consumed: OnPersistedItemConsumed));
            lock (inventoryGate)
            {
                if (added.Any(item => items.ContainsKey(item.Id))) throw new InvalidOperationException("Account loot collided with another accepted item.");
                foreach (var item in added) items.Add(item.Id, item);
            }
            foreach (var item in added) SendItemAdd(item);
            return added;
        }

        internal void AdoptCraftingItems(IReadOnlyList<AccountInventoryModel> rows, IGameSession session, Func<bool> owns)
        {
            var added = new List<AccountInventoryItem>();
            lock (inventoryGate)
            {
                if (!owns() || !ReferenceEquals(account.Session, session)) throw new InvalidOperationException("Crafting lost its account inventory owner.");
                foreach (var row in rows)
                {
                    if (row.Id != account.Id || row.InventoryId == 0 || string.IsNullOrEmpty(row.CreationKey)
                        || items.TryGetValue(row.InventoryId, out var prior) && (prior is not AccountInventoryItem item || item.ConsumptionKey != row.CreationKey))
                        throw new InvalidOperationException("Crafting account inventory collided with another accepted item.");
                }
                foreach (var row in rows)
                {
                    if (items.ContainsKey(row.InventoryId)) continue;
                    var item = new AccountInventoryItem(account, row, gameTableManager, OnPersistedItemConsumed);
                    items.Add(item.Id, item); added.Add(item);
                    nextInventoryId = Math.Max(nextInventoryId, checked(item.Id + 1ul));
                }
            }
            foreach (var item in added)
            {
                if (!owns()) return;
                SendItemCacheAdd(item, session);
                if (!owns()) return;
                SendItemAddPacket(item, session);
            }
        }
    }
}
