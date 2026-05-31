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
            log.LogInformation("StorefrontCatalogDiagnostics account item take request player={PlayerGuid} account={AccountId} inventoryId={InventoryId}.",
                session.Player?.Guid, session.Account?.Id, accountItemTake.Id);

            AccountOperationResult result = session.Account.InventoryManager.TakeItem(session.Player, accountItemTake.Id);

            log.LogInformation("StorefrontCatalogDiagnostics account item take result player={PlayerGuid} account={AccountId} inventoryId={InventoryId} result={Result}.",
                session.Player?.Guid, session.Account?.Id, accountItemTake.Id, result);

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
            log.LogInformation("StorefrontCatalogDiagnostics pending account item claim request player={PlayerGuid} account={AccountId} group={Group}.",
                session.Player?.Guid, session.Account?.Id, claimPendingItemGroup.Group);

            AccountOperationResult result = session.Account.InventoryManager.ClaimPendingItemGroup(session.Player, claimPendingItemGroup.Group);

            log.LogInformation("StorefrontCatalogDiagnostics pending account item claim result player={PlayerGuid} account={AccountId} group={Group} result={Result}.",
                session.Player?.Guid, session.Account?.Id, claimPendingItemGroup.Group, result);

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
            log.LogInformation("StorefrontCatalogDiagnostics pending account item return request player={PlayerGuid} account={AccountId} group={Group}.",
                session.Player?.Guid, session.Account?.Id, returnPendingItemGroup.Group);

            AccountOperationResult result = session.Account.InventoryManager.ReturnPendingItemGroup(session.Player, returnPendingItemGroup.Group);

            log.LogInformation("StorefrontCatalogDiagnostics pending account item return result player={PlayerGuid} account={AccountId} group={Group} result={Result}.",
                session.Player?.Guid, session.Account?.Id, returnPendingItemGroup.Group, result);

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
            log.LogInformation("StorefrontCatalogDiagnostics pending account item character gift request player={PlayerGuid} account={AccountId} group={Group} target={TargetCharacter}.",
                session.Player?.Guid, session.Account?.Id, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter);

            AccountOperationResult result = session.Account.InventoryManager.GiftPendingItemGroupToCharacter(session.Player, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter);

            log.LogInformation("StorefrontCatalogDiagnostics pending account item character gift result player={PlayerGuid} account={AccountId} group={Group} target={TargetCharacter} result={Result}.",
                session.Player?.Guid, session.Account?.Id, giftPendingItemGroup.Group, giftPendingItemGroup.TargetCharacter, result);

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
            log.LogInformation("StorefrontCatalogDiagnostics pending account item account gift request player={PlayerGuid} account={AccountId} group={Group} targetAccount={TargetAccountId} reservedZero={ReservedZero} sender={SenderCharacter}.",
                session.Player?.Guid, session.Account?.Id, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.ReservedZero, giftPendingItemGroup.SenderCharacter);

            if (giftPendingItemGroup.ReservedZero != 0u)
            {
                log.LogDebug("Rejecting pending account item group account gift from player {PlayerGuid}: group {Group}, target account {TargetAccountId}, reservedZero {ReservedZero}, sender {SenderCharacter}.",
                    session.Player?.Guid, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.ReservedZero, giftPendingItemGroup.SenderCharacter);

                session.Account.InventoryManager.SendPendingItems();
                ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GiftItem, AccountOperationResult.GenericFail);
                return;
            }

            AccountOperationResult result = session.Account.InventoryManager.GiftPendingItemGroupToAccount(session.Player, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.SenderCharacter);

            log.LogInformation("StorefrontCatalogDiagnostics pending account item account gift result player={PlayerGuid} account={AccountId} group={Group} targetAccount={TargetAccountId} reservedZero={ReservedZero} sender={SenderCharacter} result={Result}.",
                session.Player?.Guid, session.Account?.Id, giftPendingItemGroup.Group, giftPendingItemGroup.TargetAccountId, giftPendingItemGroup.ReservedZero, giftPendingItemGroup.SenderCharacter, result);

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
