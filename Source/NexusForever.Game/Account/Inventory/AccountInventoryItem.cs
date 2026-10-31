using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using ServerAccountInventoryItem = NexusForever.Network.World.Message.Model.Shared.AccountInventoryItem;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public class AccountInventoryItem : IAccountInventoryItem
    {
        [Flags]
        private enum AccountInventorySaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Update = 0x0002,
            Delete = 0x0004
        }

        public ulong Id { get; }
        public uint AccountItemId { get; }
        public bool ForceSoulbind { get; }
        public string ConsumptionKey => deliveryKey;
        public AccountItemEntry Entry { get; }

        public AccountItemClaimState ClaimState
        {
            get => claimState;
            set
            {
                lock (saveGate)
                {
                    mutationRevision++;
                    claimState = value;
                    if ((saveMask & AccountInventorySaveMask.Create) == 0)
                        saveMask |= AccountInventorySaveMask.Update;
                }
            }
        }

        public bool HasTargetPlayerIdentity
        {
            get => hasTargetPlayerIdentity;
            set
            {
                lock (saveGate)
                {
                    mutationRevision++;
                    hasTargetPlayerIdentity = value;
                    if ((saveMask & AccountInventorySaveMask.Create) == 0)
                        saveMask |= AccountInventorySaveMask.Update;
                }
            }
        }

        public NetworkIdentity TargetPlayerIdentity { get; } = new();

        public bool PendingCreate => (saveMask & AccountInventorySaveMask.Create) != 0;
        public bool PendingDelete => (saveMask & AccountInventorySaveMask.Delete) != 0;

        private AccountItemClaimState claimState;
        private bool hasTargetPlayerIdentity;
        private volatile AccountInventorySaveMask saveMask;
        private readonly object saveGate = new();
        private readonly string deliveryKey = $"account-item:{Guid.NewGuid():N}";
        private bool saveInFlight;
        private ulong mutationRevision;
        private readonly Action<ulong> consumed;

        private readonly IAccount account;

        public AccountInventoryItem(IAccount account, AccountInventoryModel model, IGameTableManager gameTableManager, Action<ulong> consumed = null)
        {
            this.consumed = consumed;
            this.account = account;

            Id            = model.InventoryId;
            deliveryKey   = model.CreationKey ?? $"legacy:{model.InventoryId}";
            AccountItemId = model.AccountItemId;
            ForceSoulbind = model.ForceSoulbind;
            claimState    = (AccountItemClaimState)model.ClaimState;
            hasTargetPlayerIdentity = model.HasTargetPlayerIdentity || model.TargetCharacterId != 0ul;

            TargetPlayerIdentity.RealmId = model.TargetRealmId;
            TargetPlayerIdentity.Id      = model.TargetCharacterId;

            Entry = gameTableManager.AccountItem?.GetEntry(AccountItemId);
            if (Entry == null)
                throw new ArgumentException($"Account item {AccountItemId} does not exist!");

            saveMask = hasTargetPlayerIdentity == model.HasTargetPlayerIdentity
                ? AccountInventorySaveMask.None
                : AccountInventorySaveMask.Update;
        }

        public AccountInventoryItem(IAccount account, ulong id, uint accountItemId, NetworkIdentity targetPlayerIdentity, AccountItemClaimState claimState, bool hasTargetPlayerIdentity, IGameTableManager gameTableManager, bool forceSoulbind = false, Action<ulong> consumed = null)
        {
            this.consumed = consumed;
            this.account = account;

            Id            = id;
            AccountItemId = accountItemId;
            ForceSoulbind = forceSoulbind;
            this.claimState = claimState;
            this.hasTargetPlayerIdentity = hasTargetPlayerIdentity || targetPlayerIdentity?.Id != 0ul;

            if (targetPlayerIdentity != null)
            {
                TargetPlayerIdentity.RealmId = targetPlayerIdentity.RealmId;
                TargetPlayerIdentity.Id      = targetPlayerIdentity.Id;
            }

            Entry = gameTableManager.AccountItem?.GetEntry(AccountItemId);
            if (Entry == null)
                throw new ArgumentException($"Account item {AccountItemId} does not exist!");

            saveMask = AccountInventorySaveMask.Create;
        }

        public void Save(AuthContext context)
        {
            lock (saveGate)
            {
                if (saveInFlight || saveMask == AccountInventorySaveMask.None) return;
                AccountInventorySaveMask captured = saveMask;
                ulong revision = mutationRevision;
                AccountInventoryModel snapshot = BuildModel();
                var staged = new List<object>();
                bool alreadyConsumed = false;
                saveInFlight = true;
                try
                {
                    context.EnlistSaveWork(unit =>
                    {
                        foreach (object entity in staged) unit.Entry(entity).State = EntityState.Detached;
                        staged.Clear();
                        unit.ReservePendingItemIds(account.Id, 0);
                        bool mysql = unit.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
                        AccountInventoryModel row = (mysql
                            ? unit.AccountInventory.FromSqlInterpolated($"SELECT * FROM account_inventory WHERE id = {account.Id} AND inventoryId = {Id} FOR UPDATE")
                            : unit.AccountInventory.Where(item => item.Id == account.Id && item.InventoryId == Id)).AsNoTracking().SingleOrDefault();
                        alreadyConsumed = false;
                        if (row != null && (row.CreationKey ?? $"legacy:{row.InventoryId}") != deliveryKey)
                        {
                            if ((captured & AccountInventorySaveMask.Create) == 0)
                            {
                                alreadyConsumed = true;
                                return;
                            }
                            bool previouslyCreated = (mysql
                                ? unit.AccountInventoryReceipt.FromSqlInterpolated($"SELECT * FROM account_inventory_receipt WHERE accountId = {account.Id} AND deliveryKey = {deliveryKey} FOR UPDATE")
                                : unit.AccountInventoryReceipt.Where(item => item.AccountId == account.Id && item.DeliveryKey == deliveryKey)).AsNoTracking().Any();
                            if (previouslyCreated) { alreadyConsumed = true; return; }
                            if ((captured & AccountInventorySaveMask.Delete) != 0) return;
                            throw new InvalidOperationException("The account inventory identity is already occupied by another creation.");
                        }
                        if ((captured & AccountInventorySaveMask.Delete) != 0)
                        {
                            if ((captured & AccountInventorySaveMask.Create) != 0)
                            {
                                bool created = (mysql
                                    ? unit.AccountInventoryReceipt.FromSqlInterpolated($"SELECT * FROM account_inventory_receipt WHERE accountId = {account.Id} AND deliveryKey = {deliveryKey} FOR UPDATE")
                                    : unit.AccountInventoryReceipt.Where(item => item.AccountId == account.Id && item.DeliveryKey == deliveryKey)).AsNoTracking().Any();
                                if (!created) return;
                            }
                            if (row != null && row.AccountItemId != AccountItemId) throw new InvalidOperationException("The account inventory item identity changed.");
                            if (row != null) { unit.Remove(row); staged.Add(row); }
                            return;
                        }
                        if ((captured & AccountInventorySaveMask.Create) != 0)
                        {
                            AccountInventoryReceiptModel receipt = (mysql
                                ? unit.AccountInventoryReceipt.FromSqlInterpolated($"SELECT * FROM account_inventory_receipt WHERE accountId = {account.Id} AND deliveryKey = {deliveryKey} FOR UPDATE")
                                : unit.AccountInventoryReceipt.Where(item => item.AccountId == account.Id && item.DeliveryKey == deliveryKey)).AsNoTracking().SingleOrDefault();
                            if (receipt == null)
                            {
                                if (row != null) throw new InvalidOperationException("The account inventory identity is already occupied.");
                                row = snapshot;
                                unit.AccountInventory.Add(row);
                                receipt = new AccountInventoryReceiptModel { AccountId = account.Id, DeliveryKey = deliveryKey, InventoryId = Id };
                                unit.AccountInventoryReceipt.Add(receipt);
                                staged.Add(row); staged.Add(receipt);
                                return;
                            }
                            if (receipt.InventoryId != Id) throw new InvalidOperationException("The account inventory delivery identity changed.");
                        }
                        if (row == null) { alreadyConsumed = true; return; }
                        if (row.AccountItemId != AccountItemId) throw new InvalidOperationException("The account inventory item identity changed.");
                        unit.Attach(row);
                        row.ClaimState = snapshot.ClaimState; row.HasTargetPlayerIdentity = snapshot.HasTargetPlayerIdentity;
                        row.TargetRealmId = snapshot.TargetRealmId; row.TargetCharacterId = snapshot.TargetCharacterId;
                        staged.Add(row);
                    }, committed =>
                    {
                        bool notifyConsumed = false;
                        lock (saveGate)
                        {
                            saveInFlight = false;
                            foreach (object entity in staged)
                            {
                                try { context.Entry(entity).State = EntityState.Detached; }
                                catch (ObjectDisposedException) { }
                            }
                            if (committed)
                            {
                                saveMask &= ~captured;
                                if (mutationRevision != revision && (saveMask & AccountInventorySaveMask.Delete) == 0)
                                    saveMask |= AccountInventorySaveMask.Update;
                                notifyConsumed = alreadyConsumed;
                            }
                        }
                        if (notifyConsumed) consumed?.Invoke(Id);
                    });
                }
                catch { saveInFlight = false; throw; }
            }
        }

        public void EnqueueDelete(bool set)
        {
            lock (saveGate)
            {
                mutationRevision++;
                if (set)
                    saveMask |= AccountInventorySaveMask.Delete;
                else
                    saveMask &= ~AccountInventorySaveMask.Delete;
            }
        }

        public ServerAccountInventoryItem Build()
        {
            return new ServerAccountInventoryItem
            {
                Id                   = Id,
                ItemId               = AccountItemId,
                ClaimState           = ClaimState,
                HasTargetPlayerIdentity = HasTargetPlayerIdentity,
                TargetPlayerIdentity = new NetworkIdentity
                {
                    RealmId = TargetPlayerIdentity.RealmId,
                    Id      = TargetPlayerIdentity.Id
                }
            };
        }

        private AccountInventoryModel BuildModel()
        {
            return new AccountInventoryModel
            {
                Id                = account.Id,
                InventoryId       = Id,
                AccountItemId     = AccountItemId,
                ForceSoulbind     = ForceSoulbind,
                CreationKey       = deliveryKey,
                ClaimState        = (byte)ClaimState,
                HasTargetPlayerIdentity = HasTargetPlayerIdentity,
                TargetRealmId     = TargetPlayerIdentity.RealmId,
                TargetCharacterId = TargetPlayerIdentity.Id
            };
        }
    }
}
