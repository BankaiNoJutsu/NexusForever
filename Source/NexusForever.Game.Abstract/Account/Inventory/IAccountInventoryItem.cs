using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using ServerAccountInventoryItem = NexusForever.Network.World.Message.Model.Shared.AccountInventoryItem;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Abstract.Account.Inventory
{
    public interface IAccountInventoryItem : IDatabaseAuth, IDatabaseState, INetworkBuildable<ServerAccountInventoryItem>
    {
        ulong Id { get; }
        uint AccountItemId { get; }
        AccountItemClaimState ClaimState { get; set; }
        bool Unknown1 { get; set; }
        NetworkIdentity TargetPlayerIdentity { get; }
        AccountItemEntry Entry { get; }
    }
}
