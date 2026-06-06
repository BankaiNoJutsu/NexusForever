using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static.Storefront;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Account;
using NexusForever.WorldServer.Network.Message.Handler.Character;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientStorefrontPurchaseCharacterHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseCharacter>
    {
        private readonly ILogger<ClientStorefrontPurchaseCharacterHandler> log;
        private readonly IStorefrontPurchaseService storefrontPurchaseService;
        private readonly IGameTableManager gameTableManager;
        private readonly ICharacterListManager characterListManager;

        public ClientStorefrontPurchaseCharacterHandler(
            ILogger<ClientStorefrontPurchaseCharacterHandler> log,
            IStorefrontPurchaseService storefrontPurchaseService,
            IGameTableManager gameTableManager,
            ICharacterListManager characterListManager)
        {
            this.log                       = log;
            this.storefrontPurchaseService = storefrontPurchaseService;
            this.gameTableManager          = gameTableManager;
            this.characterListManager      = characterListManager;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseCharacter purchase)
        {
            log.LogInformation("StorefrontCatalogDiagnostics storefront character purchase request player={PlayerGuid} account={AccountId} offer={OfferId} currency={CurrencyId} slot={PaymentCurrencySlot} amountBits={PurchaseMoneyAmountBits} option={PurchaseOptionId} extension={PurchaseExtensionId} target={Target}.",
                session.Player?.Guid, session.Account?.Id, purchase.OfferId, purchase.CurrencyId, purchase.PaymentCurrencySlot, purchase.PurchaseMoneyAmountBits, purchase.PurchaseOptionId, purchase.PurchaseExtensionId, purchase.Target);

            bool characterSelectPurchase = session.Player == null;
            if (!characterSelectPurchase && !storefrontPurchaseService.IsCurrentOrEmptyTarget(session, purchase.Target))
            {
                log.LogWarning("Rejecting storefront character purchase from player {PlayerGuid}: non-current target {Target}.",
                    session.Player?.Guid, purchase.Target);
                storefrontPurchaseService.SendFailure(session, StoreError.GenericFail);
                return;
            }

            DirectAccountGrantPlan directGrantPlan = null;
            storefrontPurchaseService.TryPurchase(session, log,
                purchase.OfferId, purchase.PaymentCurrencySlot, purchase.CurrencyId,
                (offerItem, accountItemIds) =>
                {
                    log.LogInformation("StorefrontCatalogDiagnostics storefront character purchase delivery player={PlayerGuid} account={AccountId} itemCount={AccountItemCount} items=[{AccountItems}].",
                        session.Player?.Guid, session.Account?.Id, accountItemIds.Count, string.Join(",", accountItemIds));
                    if (directGrantPlan != null)
                    {
                        storefrontPurchaseService.ApplyDirectAccountGrantPlan(session, directGrantPlan);
                        characterListManager.SendCharacterListPackets(session);
                    }
                    else
                    {
                        NetworkIdentity targetPlayerIdentity = storefrontPurchaseService.GetCurrentPlayerIdentity(session);
                        foreach (uint accountItemId in accountItemIds)
                            session.Account.InventoryManager.AddItem(accountItemId, targetPlayerIdentity, hasTargetPlayerIdentity: targetPlayerIdentity.Id != 0ul);
                    }

                    storefrontPurchaseService.SendCharacterPurchaseSuccess(session);
                },
                characterSelectPurchase ? "character select direct account" : "character",
                characterSelectPurchase
                    ? (IOfferItem offerItem, IReadOnlyList<uint> _, out StoreError error, out string reason) =>
                        storefrontPurchaseService.TryBuildDirectAccountGrantPlan(session, gameTableManager, offerItem, requireDirectAccountGrant: true, out directGrantPlan, out error, out reason)
                    : null,
                requirePlayer: !characterSelectPurchase);
        }
    }

    public class ClientStorefrontPurchaseAccountHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseAccount>
    {
        private readonly ILogger<ClientStorefrontPurchaseAccountHandler> log;
        private readonly IStorefrontPurchaseService storefrontPurchaseService;
        private readonly ICharacterManager characterManager;
        private readonly IPlayerManager playerManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IAccountPendingItemRepository pendingItemRepository;
        private readonly ICharacterListManager characterListManager;

        public ClientStorefrontPurchaseAccountHandler(
            ILogger<ClientStorefrontPurchaseAccountHandler> log,
            IStorefrontPurchaseService storefrontPurchaseService,
            ICharacterManager characterManager,
            IPlayerManager playerManager,
            IGameTableManager gameTableManager,
            IAccountPendingItemRepository pendingItemRepository,
            ICharacterListManager characterListManager)
        {
            this.log                       = log;
            this.storefrontPurchaseService = storefrontPurchaseService;
            this.characterManager          = characterManager;
            this.playerManager             = playerManager;
            this.gameTableManager          = gameTableManager;
            this.pendingItemRepository     = pendingItemRepository;
            this.characterListManager      = characterListManager;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseAccount purchase)
        {
            log.LogInformation("StorefrontCatalogDiagnostics storefront account purchase request player={PlayerGuid} account={AccountId} offer={OfferId} currency={CurrencyId} slot={PaymentCurrencySlot} amountBits={PurchaseMoneyAmountBits} option={PurchaseOptionId} extension={PurchaseExtensionId} accountExtension={AccountPurchaseExtensionId} target={Target} accountTarget={AccountTarget} recipientLength={RecipientLength}.",
                session.Player?.Guid, session.Account?.Id, purchase.OfferId, purchase.CurrencyId, purchase.PaymentCurrencySlot, purchase.PurchaseMoneyAmountBits, purchase.PurchaseOptionId, purchase.PurchaseExtensionId, purchase.AccountPurchaseExtensionId, purchase.Target, purchase.AccountTarget, purchase.RecipientName?.Length ?? 0);

            if (!storefrontPurchaseService.IsCurrentOrEmptyTarget(session, purchase.Target))
            {
                log.LogDebug("Rejecting storefront account purchase from player {PlayerGuid}: non-current target {Target}, account target {AccountTarget}, recipient length {RecipientLength}.",
                    session.Player?.Guid, purchase.Target, purchase.AccountTarget, purchase.RecipientName?.Length ?? 0);
                storefrontPurchaseService.SendFailure(session, StoreError.GenericFail);
                return;
            }

            bool hasRecipientName = !string.IsNullOrWhiteSpace(purchase.RecipientName);
            bool hasAccountTarget = purchase.AccountTarget.Id != 0ul && !storefrontPurchaseService.IsCurrentOrEmptyTarget(session, purchase.AccountTarget);
            if (hasRecipientName || hasAccountTarget)
            {
                if (!TryResolveGiftRecipient(session, purchase, out uint recipientAccountId, out IPlayer recipient, out NetworkIdentity recipientIdentity))
                    return;

                storefrontPurchaseService.TryPurchase(session, log,
                    purchase.OfferId, purchase.PaymentCurrencySlot, purchase.CurrencyId,
                    (_, accountItemIds) =>
                    {
                        log.LogInformation("StorefrontCatalogDiagnostics storefront account gift delivery player={PlayerGuid} account={AccountId} recipientAccount={RecipientAccountId} online={IsOnline} itemCount={AccountItemCount} items=[{AccountItems}].",
                            session.Player?.Guid, session.Account?.Id, recipientAccountId, recipient != null, accountItemIds.Count, string.Join(",", accountItemIds));
                        DeliverStorefrontGift(session, recipientAccountId, recipient, recipientIdentity, accountItemIds);
                        storefrontPurchaseService.SendAccountPurchaseSuccess(session);
                    },
                    "account gift");
                return;
            }

            DirectAccountGrantPlan directGrantPlan = null;
            bool characterSelectPurchase = session.Player == null;
            storefrontPurchaseService.TryPurchase(session, log,
                purchase.OfferId, purchase.PaymentCurrencySlot, purchase.CurrencyId,
                (offerItem, accountItemIds) =>
                {
                    log.LogInformation("StorefrontCatalogDiagnostics storefront account purchase delivery player={PlayerGuid} account={AccountId} itemCount={AccountItemCount} items=[{AccountItems}].",
                        session.Player?.Guid, session.Account?.Id, accountItemIds.Count, string.Join(",", accountItemIds));
                    if (directGrantPlan != null)
                    {
                        storefrontPurchaseService.ApplyDirectAccountGrantPlan(session, directGrantPlan);
                        characterListManager.SendCharacterListPackets(session);
                    }
                    else
                    {
                        foreach (uint accountItemId in accountItemIds)
                            session.Account.InventoryManager.AddItem(accountItemId);
                    }

                    storefrontPurchaseService.SendAccountPurchaseSuccess(session);
                },
                "account",
                characterSelectPurchase
                    ? (IOfferItem offerItem, IReadOnlyList<uint> _, out StoreError error, out string reason) =>
                        storefrontPurchaseService.TryBuildDirectAccountGrantPlan(session, gameTableManager, offerItem, requireDirectAccountGrant: true, out directGrantPlan, out error, out reason)
                    : (IOfferItem offerItem, IReadOnlyList<uint> _, out StoreError error, out string reason) =>
                        storefrontPurchaseService.ValidateDirectAccountGrantClaim(session, gameTableManager, offerItem, out error, out reason),
                requirePlayer: false);
        }

        private bool TryResolveGiftRecipient(IWorldSession session, ClientStorefrontPurchaseAccount purchase, out uint recipientAccountId, out IPlayer recipient, out NetworkIdentity recipientIdentity)
        {
            recipientAccountId = 0u;
            recipient          = null;
            recipientIdentity  = null;

            ICharacter character = !string.IsNullOrWhiteSpace(purchase.RecipientName)
                ? characterManager.GetCharacter(purchase.RecipientName)
                : characterManager.GetCharacter(purchase.AccountTarget.Id);

            if (character == null)
            {
                log.LogDebug("Rejecting storefront account gift from player {PlayerGuid}: unknown recipient name {RecipientName}, account target {AccountTarget}.",
                    session.Player?.Guid, purchase.RecipientName, purchase.AccountTarget);
                storefrontPurchaseService.SendFailure(session, StoreError.IneligibleGiftRecipient);
                return false;
            }

            recipientAccountId = character.AccountId;
            recipient          = playerManager.GetPlayerByAccountId(character.AccountId);
            recipientIdentity  = new NetworkIdentity
            {
                RealmId = RealmContext.Instance.RealmId,
                Id      = character.CharacterId
            };

            return true;
        }

        private void DeliverStorefrontGift(IWorldSession session, uint recipientAccountId, IPlayer recipient, NetworkIdentity recipientIdentity, IReadOnlyList<uint> accountItemIds)
        {
            if (recipient != null)
            {
                recipient.Account.InventoryManager.AddPendingItemGroup(
                    accountItemIds,
                    storefrontPurchaseService.GetCurrentPlayerIdentity(session),
                    recipientIdentity,
                    senderAccountId: session.Account.Id,
                    targetAccountId: recipientAccountId);
                return;
            }

            pendingItemRepository.AppendPendingGroup(recipientAccountId, new AccountPendingItemInsert
            {
                AccountItemIds  = accountItemIds,
                SenderAccountId = session.Account.Id,
                SenderIdentity  = storefrontPurchaseService.GetCurrentPlayerIdentity(session),
                TargetIdentity  = recipientIdentity
            });

            log.LogDebug("Queued storefront gift from player {PlayerGuid} to offline account {RecipientAccountId}.",
                session.Player?.Guid, recipientAccountId);
        }
    }
}
