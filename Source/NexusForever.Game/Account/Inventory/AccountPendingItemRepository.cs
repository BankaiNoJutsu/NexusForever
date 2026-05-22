using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Static.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public sealed class AccountPendingItemRepository : IAccountPendingItemRepository
    {
        public void AppendPendingGroup(uint targetAccountId, AccountPendingItemInsert insert)
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

            DatabaseManager.Instance.GetDatabase<AuthDatabase>().Save(context =>
            {
                ulong nextPendingItemId = context.AccountPendingItem
                    .Where(p => p.Id == targetAccountId)
                    .Select(p => (ulong?)p.PendingItemId)
                    .Max() ?? 0ul;
                nextPendingItemId++;

                foreach (uint accountItemId in insert.AccountItemIds)
                {
                    context.AccountPendingItem.Add(new AccountPendingItemModel
                    {
                        Id                = targetAccountId,
                        PendingItemId     = nextPendingItemId++,
                        GroupName         = groupName,
                        AccountItemId     = accountItemId,
                        SenderAccountId   = insert.SenderAccountId,
                        SenderRealmId     = senderIdentity.RealmId,
                        SenderCharacterId = senderIdentity.Id,
                        TargetRealmId     = targetIdentity.RealmId,
                        TargetCharacterId = targetIdentity.Id,
                        ClaimState        = (byte)insert.ClaimState,
                        Unknown1          = insert.Unknown1
                    });
                }
            }).GetAwaiter().GetResult();
        }
    }
}
