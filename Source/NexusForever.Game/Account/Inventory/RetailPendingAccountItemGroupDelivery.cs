using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    /// <summary>
    /// Delivers pending account item groups to online recipients immediately, or persists them for offline accounts.
    /// </summary>
    public sealed class RetailPendingAccountItemGroupDelivery : IPendingAccountItemGroupDelivery
    {
        private readonly IAccountPendingItemRepository pendingItemRepository;
        private readonly ILogger<RetailPendingAccountItemGroupDelivery> log;

        public RetailPendingAccountItemGroupDelivery(
            IAccountPendingItemRepository pendingItemRepository,
            ILogger<RetailPendingAccountItemGroupDelivery> log)
        {
            this.pendingItemRepository = pendingItemRepository;
            this.log                   = log;
        }

        public AccountOperationResult Deliver(PendingAccountItemGroupDeliveryRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.AccountItemIds);

            IPlayer targetPlayer = PlayerManager.Instance.GetPlayerByAccountId(request.TargetAccountId);
            if (targetPlayer != null)
            {
                targetPlayer.Account.InventoryManager.AddPendingItemGroup(
                    request.AccountItemIds,
                    request.SenderIdentity,
                    request.TargetIdentity,
                    request.SourceGroup,
                    senderAccountId: request.SourceAccountId,
                    targetAccountId: request.TargetAccountId);

                return AccountOperationResult.Ok;
            }

            uint persistedSenderAccountId = request.TransferKind == PendingAccountItemGroupTransferKind.ReturnToSender
                ? request.TargetAccountId
                : request.SourceAccountId;

            pendingItemRepository.AppendPendingGroup(request.TargetAccountId, new AccountPendingItemInsert
            {
                GroupName        = request.SourceGroup,
                AccountItemIds   = request.AccountItemIds,
                SenderAccountId  = persistedSenderAccountId,
                SenderIdentity = request.SenderIdentity,
                TargetIdentity = request.TargetIdentity,
                HasTargetPlayerIdentity = request.TargetIdentity?.Id != 0ul
            });

            log.LogDebug(
                "Persisted pending account item group {Group} for offline account {TargetAccountId} from account {SourceAccountId} ({TransferKind}).",
                request.SourceGroup,
                request.TargetAccountId,
                request.SourceAccountId,
                request.TransferKind);

            return AccountOperationResult.Ok;
        }
    }
}
