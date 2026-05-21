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
            AccountOperationResult result = session.Account.InventoryManager.ClaimPendingItemGroup(session.Player, claimPendingItemGroup.Group);
            if (result != AccountOperationResult.Ok)
            {
                log.LogDebug("Rejecting pending account item group claim from player {PlayerGuid}: group {Group}, result {Result}.",
                    session.Player?.Guid, claimPendingItemGroup.Group, result);
            }
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
            AccountOperationResult result = session.Account.InventoryManager.ReturnPendingItemGroup(session.Player, returnPendingItemGroup.Group);
            if (result != AccountOperationResult.Ok)
            {
                log.LogDebug("Rejecting pending account item group return from player {PlayerGuid}: group {Group}, result {Result}.",
                    session.Player?.Guid, returnPendingItemGroup.Group, result);
            }
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
            AccountOperationResult result = session.Account.InventoryManager.GiftPendingItemGroupToCharacter(session.Player, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter);
            if (result != AccountOperationResult.Ok)
            {
                log.LogDebug("Rejecting pending account item group character gift from player {PlayerGuid}: group {Group}, target {TargetCharacter}, result {Result}.",
                    session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter, result);
            }
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
            if (giftPendingItemGroup.ReservedZero != 0u)
            {
                log.LogDebug("Rejecting pending account item group account gift from player {PlayerGuid}: group {Group}, target account {TargetAccountId}, reservedZero {ReservedZero}, sender {SenderCharacter}.",
                    session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.ReservedZero, giftPendingItemGroup.SenderCharacter);

                session.Account.InventoryManager.SendPendingItems();
                ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GiftItem, AccountOperationResult.GenericFail);
                return;
            }

            AccountOperationResult result = session.Account.InventoryManager.GiftPendingItemGroupToAccount(session.Player, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.SenderCharacter);
            if (result != AccountOperationResult.Ok)
            {
                log.LogDebug("Rejecting pending account item group account gift from player {PlayerGuid}: group {Group}, target account {TargetAccountId}, reservedZero {ReservedZero}, sender {SenderCharacter}, result {Result}.",
                    session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.ReservedZero, giftPendingItemGroup.SenderCharacter, result);
            }
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
