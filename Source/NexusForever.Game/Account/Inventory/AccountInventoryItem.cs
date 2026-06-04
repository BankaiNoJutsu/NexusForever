using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public AccountItemEntry Entry { get; }

        public AccountItemClaimState ClaimState
        {
            get => claimState;
            set
            {
                claimState = value;
                if ((saveMask & AccountInventorySaveMask.Create) == 0)
                    saveMask |= AccountInventorySaveMask.Update;
            }
        }

        public bool HasTargetPlayerIdentity
        {
            get => hasTargetPlayerIdentity;
            set
            {
                hasTargetPlayerIdentity = value;
                if ((saveMask & AccountInventorySaveMask.Create) == 0)
                    saveMask |= AccountInventorySaveMask.Update;
            }
        }

        public NetworkIdentity TargetPlayerIdentity { get; } = new();

        public bool PendingCreate => (saveMask & AccountInventorySaveMask.Create) != 0;
        public bool PendingDelete => (saveMask & AccountInventorySaveMask.Delete) != 0;

        private AccountItemClaimState claimState;
        private bool hasTargetPlayerIdentity;
        private AccountInventorySaveMask saveMask;

        private readonly IAccount account;

        public AccountInventoryItem(IAccount account, AccountInventoryModel model)
        {
            this.account = account;

            Id            = model.InventoryId;
            AccountItemId = model.AccountItemId;
            claimState    = (AccountItemClaimState)model.ClaimState;
            hasTargetPlayerIdentity = model.HasTargetPlayerIdentity || model.TargetCharacterId != 0ul;

            TargetPlayerIdentity.RealmId = model.TargetRealmId;
            TargetPlayerIdentity.Id      = model.TargetCharacterId;

            Entry = GameTableManager.Instance.AccountItem.GetEntry(AccountItemId);
            if (Entry == null)
                throw new ArgumentException($"Account item {AccountItemId} does not exist!");

            saveMask = hasTargetPlayerIdentity == model.HasTargetPlayerIdentity
                ? AccountInventorySaveMask.None
                : AccountInventorySaveMask.Update;
        }

        public AccountInventoryItem(IAccount account, ulong id, uint accountItemId, NetworkIdentity targetPlayerIdentity, AccountItemClaimState claimState, bool hasTargetPlayerIdentity)
        {
            this.account = account;

            Id            = id;
            AccountItemId = accountItemId;
            this.claimState = claimState;
            this.hasTargetPlayerIdentity = hasTargetPlayerIdentity || targetPlayerIdentity?.Id != 0ul;

            if (targetPlayerIdentity != null)
            {
                TargetPlayerIdentity.RealmId = targetPlayerIdentity.RealmId;
                TargetPlayerIdentity.Id      = targetPlayerIdentity.Id;
            }

            Entry = GameTableManager.Instance.AccountItem.GetEntry(AccountItemId);
            if (Entry == null)
                throw new ArgumentException($"Account item {AccountItemId} does not exist!");

            saveMask = AccountInventorySaveMask.Create;
        }

        public void Save(AuthContext context)
        {
            if (saveMask == AccountInventorySaveMask.None)
                return;

            AccountInventoryModel model = BuildModel();

            if ((saveMask & AccountInventorySaveMask.Create) != 0)
            {
                context.Add(model);
            }
            else if ((saveMask & AccountInventorySaveMask.Delete) != 0)
            {
                context.Remove(model);
            }
            else if ((saveMask & AccountInventorySaveMask.Update) != 0)
            {
                EntityEntry<AccountInventoryModel> entity = context.Attach(model);
                entity.Property(p => p.ClaimState).IsModified = true;
                entity.Property(p => p.HasTargetPlayerIdentity).IsModified = true;
                entity.Property(p => p.TargetRealmId).IsModified = true;
                entity.Property(p => p.TargetCharacterId).IsModified = true;
            }

            saveMask = AccountInventorySaveMask.None;
        }

        public void EnqueueDelete(bool set)
        {
            if (set)
                saveMask |= AccountInventorySaveMask.Delete;
            else
                saveMask &= ~AccountInventorySaveMask.Delete;
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
                ClaimState        = (byte)ClaimState,
                HasTargetPlayerIdentity = HasTargetPlayerIdentity,
                TargetRealmId     = TargetPlayerIdentity.RealmId,
                TargetCharacterId = TargetPlayerIdentity.Id
            };
        }
    }
}
