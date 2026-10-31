using System.Collections.Generic;
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
        private const int AccountLockStripeCount = 256;

        private static readonly object[] accountLocks = CreateAccountLockStripes();
        private readonly IDatabaseManager databaseManager;

        public AccountPendingItemRepository(
            IDatabaseManager databaseManager)
        {
            this.databaseManager = databaseManager;
        }

        private static object[] CreateAccountLockStripes()
        {
            var stripes = new object[AccountLockStripeCount];
            for (int i = 0; i < stripes.Length; i++)
                stripes[i] = new object();

            return stripes;
        }

        private static object GetAccountLock(uint accountId) => accountLocks[accountId % AccountLockStripeCount];

        public string AppendPendingGroup(uint targetAccountId, AccountPendingItemInsert insert)
        {
            ArgumentNullException.ThrowIfNull(insert);
            ArgumentNullException.ThrowIfNull(insert.AccountItemIds);

            if (insert.AccountItemIds.Count == 0)
                throw new ArgumentException("Pending account item group must contain at least one item.", nameof(insert));
            if (insert.ForceSoulbind != null && insert.ForceSoulbind.Count != insert.AccountItemIds.Count)
                throw new ArgumentException("Binding metadata must match pending items.", nameof(insert));

            string groupName = string.IsNullOrWhiteSpace(insert.GroupName)
                ? $"pending:{targetAccountId}:{Guid.NewGuid():N}"
                : insert.GroupName;

            NetworkIdentity senderIdentity = insert.SenderIdentity ?? new NetworkIdentity();
            NetworkIdentity targetIdentity = insert.TargetIdentity ?? new NetworkIdentity();
            string[] deliveryKeys = insert.AccountItemIds.Select(_ => $"pending:{Guid.NewGuid():N}").ToArray();

            lock (GetAccountLock(targetAccountId))
            {
                databaseManager.GetDatabase<AuthDatabase>().SaveBlocking(context =>
                {
                    var staged = new List<object>();
                    context.EnlistSaveWork(unit =>
                    {
                        foreach (object row in staged)
                            unit.Entry(row).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        staged.Clear();
                        for (int index = 0; index < insert.AccountItemIds.Count; index++)
                        {
                            var row = new AccountPendingItemModel
                            {
                                Id                = targetAccountId,
                                GroupName         = groupName,
                                AccountItemId     = insert.AccountItemIds[index],
                                ForceSoulbind     = insert.ForceSoulbind?[index] ?? false,
                                SenderAccountId   = insert.SenderAccountId,
                                SenderRealmId     = senderIdentity.RealmId,
                                SenderCharacterId = senderIdentity.Id,
                                TargetRealmId     = targetIdentity.RealmId,
                                TargetCharacterId = targetIdentity.Id,
                                ClaimState              = (byte)insert.ClaimState,
                                HasTargetPlayerIdentity = insert.HasTargetPlayerIdentity
                            };
                            unit.StagePendingItemAppend(deliveryKeys[index], row);
                            if (unit.Entry(row).State == Microsoft.EntityFrameworkCore.EntityState.Added)
                            {
                                staged.Add(row);
                                staged.Add(unit.AccountPendingItemDelivery.Local.Single(receipt => receipt.AccountId == targetAccountId && receipt.DeliveryKey == deliveryKeys[index]));
                            }
                        }
                    }, _ => { });
                });
            }

            return groupName;
        }

        public void RemovePendingGroup(uint targetAccountId, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            lock (GetAccountLock(targetAccountId))
            {
                databaseManager.GetDatabase<AuthDatabase>().SaveBlocking(context =>
                {
                    context.EnlistSaveWork(unit =>
                    {
                        unit.ReservePendingItemIds(targetAccountId, 0u);
                        List<AccountPendingItemModel> rows = unit.AccountPendingItem
                            .Where(p => p.Id == targetAccountId && p.GroupName == groupName)
                            .ToList();
                        unit.AccountPendingItem.RemoveRange(rows);
                    }, _ => { });
                });
            }
        }
    }
}
