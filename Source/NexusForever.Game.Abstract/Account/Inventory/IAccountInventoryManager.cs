using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Network.World.Message.Static;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Abstract.Account.Inventory
{
    public interface IAccountInventoryManager : IDatabaseAuth, IEnumerable<IAccountInventoryItem>
    {
        IAccountInventoryItem GetItem(ulong id);
        IAccountInventoryItem AddItem(uint accountItemId, NetworkIdentity targetPlayerIdentity = null, AccountItemClaimState claimState = AccountItemClaimState.CanClaim, bool unknown1 = false, bool notify = true);
        string AddPendingItemGroup(IEnumerable<uint> accountItemIds, NetworkIdentity senderIdentity = null, NetworkIdentity targetPlayerIdentity = null, string group = null, bool notify = true, uint senderAccountId = 0u);
        bool CanAddItem(uint accountItemId);
        bool RemoveItem(ulong id);
        AccountOperationResult TakeItem(IPlayer player, ulong id);
        AccountOperationResult ClaimPendingItemGroup(IPlayer player, string group);
        AccountOperationResult ReturnPendingItemGroup(IPlayer player, string group);
        AccountOperationResult GiftPendingItemGroupToCharacter(IPlayer player, string group, NetworkIdentity targetCharacter);
        AccountOperationResult GiftPendingItemGroupToAccount(IPlayer player, string group, ulong targetAccountId, NetworkIdentity senderCharacter);
        void SendInitialPackets();
        void SendInventory();
        void SendPendingItems();
        void SendCooldowns();
    }
}
