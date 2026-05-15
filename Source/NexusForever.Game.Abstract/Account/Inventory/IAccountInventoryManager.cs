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
        bool CanAddItem(uint accountItemId);
        bool RemoveItem(ulong id);
        GenericError TakeItem(IPlayer player, ulong id);
        void SendInitialPackets();
        void SendInventory();
        void SendPendingItems();
        void SendCooldowns();
    }
}
