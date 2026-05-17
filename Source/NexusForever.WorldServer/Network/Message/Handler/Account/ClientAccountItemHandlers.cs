using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientAccountItemTakeHandler : IMessageHandler<IWorldSession, ClientAccountItemTake>
    {
        private readonly ILogger<ClientAccountItemTakeHandler> log;

        public ClientAccountItemTakeHandler(ILogger<ClientAccountItemTakeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemTake accountItemTake)
        {
            AccountOperationResult result = session.Account.InventoryManager.TakeItem(session.Player, accountItemTake.Id);

            if (result != AccountOperationResult.Ok)
            {
                log.LogDebug("Rejecting account item take from player {PlayerGuid}: account inventory id {InventoryId}, result {Result}.",
                    session.Player?.Guid, accountItemTake.Id, result);
            }
        }
    }

    public class ClientAccountItemClaimPendingItemGroupHandler : IMessageHandler<IWorldSession, ClientAccountItemClaimPendingItemGroup>
    {
        private readonly ILogger<ClientAccountItemClaimPendingItemGroupHandler> log;

        public ClientAccountItemClaimPendingItemGroupHandler(ILogger<ClientAccountItemClaimPendingItemGroupHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemClaimPendingItemGroup claimPendingItemGroup)
        {
            log.LogDebug("Rejecting pending account item group claim from player {PlayerGuid}: group {Group}, reason pending-group-store-unavailable.",
                session.Player?.Guid, claimPendingItemGroup.Group);
            session.Account.InventoryManager.SendPendingItems();
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.ClaimPending, AccountOperationResult.InvalidPendingItem);
        }
    }

    public class ClientAccountItemReturnPendingItemGroupHandler : IMessageHandler<IWorldSession, ClientAccountItemReturnPendingItemGroup>
    {
        private readonly ILogger<ClientAccountItemReturnPendingItemGroupHandler> log;

        public ClientAccountItemReturnPendingItemGroupHandler(ILogger<ClientAccountItemReturnPendingItemGroupHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemReturnPendingItemGroup returnPendingItemGroup)
        {
            log.LogDebug("Rejecting pending account item group return from player {PlayerGuid}: group {Group}, reason pending-group-store-unavailable.",
                session.Player?.Guid, returnPendingItemGroup.Group);
            session.Account.InventoryManager.SendPendingItems();
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.ReturnPending, AccountOperationResult.InvalidPendingItem);
        }
    }

    public class ClientAccountItemGiftPendingItemGroupToCharacterHandler : IMessageHandler<IWorldSession, ClientAccountItemGiftPendingItemGroupToCharacter>
    {
        private readonly ILogger<ClientAccountItemGiftPendingItemGroupToCharacterHandler> log;

        public ClientAccountItemGiftPendingItemGroupToCharacterHandler(ILogger<ClientAccountItemGiftPendingItemGroupToCharacterHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemGiftPendingItemGroupToCharacter giftPendingItemGroup)
        {
            log.LogDebug("Rejecting pending account item group character gift from player {PlayerGuid}: group {Group}, target {TargetCharacter}, reason pending-group-store-unavailable.",
                session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter);
            ClientAccountItemGiftPendingItemGroupHandler.RejectGift(session);
        }
    }

    public class ClientAccountItemGiftPendingItemGroupToAccountHandler : IMessageHandler<IWorldSession, ClientAccountItemGiftPendingItemGroupToAccount>
    {
        private readonly ILogger<ClientAccountItemGiftPendingItemGroupToAccountHandler> log;

        public ClientAccountItemGiftPendingItemGroupToAccountHandler(ILogger<ClientAccountItemGiftPendingItemGroupToAccountHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemGiftPendingItemGroupToAccount giftPendingItemGroup)
        {
            log.LogDebug("Rejecting pending account item group account gift from player {PlayerGuid}: group {Group}, target account {TargetAccountId}, unknown0 {Unknown0}, sender {SenderCharacter}, reason pending-group-store-unavailable.",
                session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.Unknown0, giftPendingItemGroup.SenderCharacter);
            ClientAccountItemGiftPendingItemGroupHandler.RejectGift(session);
        }
    }

    internal static class ClientAccountItemGiftPendingItemGroupHandler
    {
        public static void RejectGift(IWorldSession session)
        {
            session.Account.InventoryManager.SendPendingItems();
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GiftItem, AccountOperationResult.NoGifting);
        }
    }

    internal static class ClientAccountItemOperationResultHelper
    {
        public static void Send(IWorldSession session, AccountOperation operation, AccountOperationResult result)
        {
            session.EnqueueMessageEncrypted(new ServerAccountOperationResult
            {
                Operation = operation,
                Result    = result
            });
        }
    }
}
