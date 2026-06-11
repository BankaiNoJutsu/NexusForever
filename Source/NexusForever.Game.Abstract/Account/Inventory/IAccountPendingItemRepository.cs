using NexusForever.Game.Static.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Abstract.Account.Inventory
{
    public sealed class AccountPendingItemInsert
    {
        public string GroupName { get; init; }
        public IReadOnlyList<uint> AccountItemIds { get; init; }
        public uint SenderAccountId { get; init; }
        public NetworkIdentity SenderIdentity { get; init; }
        public NetworkIdentity TargetIdentity { get; init; }
        public AccountItemClaimState ClaimState { get; init; } = AccountItemClaimState.CanClaim;
        public bool HasTargetPlayerIdentity { get; init; }
    }

    public interface IAccountPendingItemRepository
    {
        string AppendPendingGroup(uint targetAccountId, AccountPendingItemInsert insert);
        void RemovePendingGroup(uint targetAccountId, string groupName);
    }
}
