using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientStorefrontPurchaseCharacterHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseCharacter>
    {
        private readonly ILogger<ClientStorefrontPurchaseCharacterHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;

        public ClientStorefrontPurchaseCharacterHandler(
            ILogger<ClientStorefrontPurchaseCharacterHandler> log,
            IGlobalStorefrontManager globalStorefrontManager)
        {
            this.log                     = log;
            this.globalStorefrontManager = globalStorefrontManager;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseCharacter purchase)
        {
            if (!StorefrontPurchaseHelper.IsCurrentOrEmptyTarget(session, purchase.Target))
            {
                log.LogWarning("Rejecting storefront character purchase from player {PlayerGuid}: non-current target {Target}.",
                    session.Player?.Guid, purchase.Target);
                StorefrontPurchaseHelper.SendFailure(session, GenericError.Params);
                return;
            }

            StorefrontPurchaseHelper.TryPurchase(session, globalStorefrontManager, log,
                purchase.OfferId, purchase.CurrencyId,
                accountItemIds =>
                {
                    NetworkIdentity targetPlayerIdentity = StorefrontPurchaseHelper.GetCurrentPlayerIdentity(session);
                    foreach (uint accountItemId in accountItemIds)
                        session.Account.InventoryManager.AddItem(accountItemId, targetPlayerIdentity);
                },
                "character");
        }
    }

    public class ClientStorefrontPurchaseAccountHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseAccount>
    {
        private readonly ILogger<ClientStorefrontPurchaseAccountHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly ICharacterManager characterManager;
        private readonly IPlayerManager playerManager;

        public ClientStorefrontPurchaseAccountHandler(
            ILogger<ClientStorefrontPurchaseAccountHandler> log,
            IGlobalStorefrontManager globalStorefrontManager,
            ICharacterManager characterManager,
            IPlayerManager playerManager)
        {
            this.log                     = log;
            this.globalStorefrontManager = globalStorefrontManager;
            this.characterManager        = characterManager;
            this.playerManager           = playerManager;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseAccount purchase)
        {
            if (!StorefrontPurchaseHelper.IsCurrentOrEmptyTarget(session, purchase.Target))
            {
                log.LogDebug("Rejecting storefront account purchase from player {PlayerGuid}: non-current target {Target}, account target {AccountTarget}, recipient length {RecipientLength}.",
                    session.Player?.Guid, purchase.Target, purchase.AccountTarget, purchase.RecipientName?.Length ?? 0);
                StorefrontPurchaseHelper.SendFailure(session, GenericError.Params);
                return;
            }

            bool hasRecipientName = !string.IsNullOrWhiteSpace(purchase.RecipientName);
            bool hasAccountTarget = purchase.AccountTarget.Id != 0ul && !StorefrontPurchaseHelper.IsCurrentOrEmptyTarget(session, purchase.AccountTarget);
            if (hasRecipientName || hasAccountTarget)
            {
                if (!TryResolveGiftRecipient(session, purchase, out IPlayer recipient, out NetworkIdentity recipientIdentity))
                    return;

                StorefrontPurchaseHelper.TryPurchase(session, globalStorefrontManager, log,
                    purchase.OfferId, purchase.CurrencyId,
                    accountItemIds => recipient.Account.InventoryManager.AddPendingItemGroup(accountItemIds, StorefrontPurchaseHelper.GetCurrentPlayerIdentity(session), recipientIdentity),
                    "account gift");
                return;
            }

            StorefrontPurchaseHelper.TryPurchase(session, globalStorefrontManager, log,
                purchase.OfferId, purchase.CurrencyId,
                accountItemIds =>
                {
                    foreach (uint accountItemId in accountItemIds)
                        session.Account.InventoryManager.AddItem(accountItemId);
                },
                "account");
        }

        private bool TryResolveGiftRecipient(IWorldSession session, ClientStorefrontPurchaseAccount purchase, out IPlayer recipient, out NetworkIdentity recipientIdentity)
        {
            recipient         = null;
            recipientIdentity = null;

            ICharacter character = !string.IsNullOrWhiteSpace(purchase.RecipientName)
                ? characterManager.GetCharacter(purchase.RecipientName)
                : characterManager.GetCharacter(purchase.AccountTarget.Id);

            if (character == null)
            {
                log.LogDebug("Rejecting storefront account gift from player {PlayerGuid}: unknown recipient name {RecipientName}, account target {AccountTarget}.",
                    session.Player?.Guid, purchase.RecipientName, purchase.AccountTarget);
                StorefrontPurchaseHelper.SendFailure(session, GenericError.Params);
                return false;
            }

            recipient = playerManager.GetPlayerByAccountId(character.AccountId);
            if (recipient == null)
            {
                log.LogDebug("Rejecting storefront account gift from player {PlayerGuid}: recipient character {CharacterId} account {AccountId} is offline.",
                    session.Player?.Guid, character.CharacterId, character.AccountId);
                StorefrontPurchaseHelper.SendFailure(session, GenericError.Params);
                return false;
            }

            recipientIdentity = new NetworkIdentity
            {
                RealmId = RealmContext.Instance.RealmId,
                Id      = character.CharacterId
            };

            return true;
        }
    }

    internal static class StorefrontPurchaseHelper
    {
        public static void TryPurchase(
            IWorldSession session,
            IGlobalStorefrontManager globalStorefrontManager,
            ILogger log,
            uint offerId,
            ushort currencyId,
            Action<IReadOnlyList<uint>> deliverItems,
            string purchaseScope)
        {
            if (session.Player == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase for offer {OfferId}: session has no player.",
                    purchaseScope, offerId);
                return;
            }

            IOfferItem offerItem = globalStorefrontManager.GetStoreOfferItem(offerId);
            if (offerItem == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: unknown offer {OfferId}.",
                    purchaseScope, session.Player.Guid, offerId);
                SendFailure(session, GenericError.ItemBadId);
                return;
            }

            var accountCurrencyType = (AccountCurrencyType)currencyId;
            if (!Enum.IsDefined(typeof(AccountCurrencyType), accountCurrencyType))
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: invalid currency {CurrencyId} for offer {OfferId}.",
                    purchaseScope, session.Player.Guid, currencyId, offerId);
                SendFailure(session, GenericError.Params);
                return;
            }

            IOfferItemPrice price = offerItem.GetPriceDataForCurrency(accountCurrencyType);
            if (price == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} has no price for currency {CurrencyId}.",
                    purchaseScope, session.Player.Guid, offerId, currencyId);
                SendFailure(session, GenericError.Params);
                return;
            }

            if (!TryGetChargeAmount(price, out ulong chargeAmount))
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} has invalid price {Price}.",
                    purchaseScope, session.Player.Guid, offerId, price.Price);
                SendFailure(session, GenericError.Params);
                return;
            }

            if (!TryBuildAccountItemList(session, offerItem, out List<uint> accountItemIds, out GenericError error, out string reason))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} cannot be placed in account inventory ({Reason}).",
                    purchaseScope, session.Player.Guid, offerId, reason);
                SendFailure(session, error);
                return;
            }

            if (chargeAmount > 0ul && !session.Account.CurrencyManager.CanAfford(accountCurrencyType, chargeAmount))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: insufficient currency {CurrencyId}, price {Price}.",
                    purchaseScope, session.Player.Guid, currencyId, chargeAmount);
                SendFailure(session, GenericError.Params);
                return;
            }

            if (chargeAmount > 0ul)
                session.Account.CurrencyManager.CurrencySubtractAmount(accountCurrencyType, chargeAmount);

            deliverItems(accountItemIds);

            log.LogDebug("Completed storefront {PurchaseScope} purchase for player {PlayerGuid}: offer {OfferId}, currency {CurrencyId}, price {Price}, account items {AccountItemCount}.",
                purchaseScope, session.Player.Guid, offerId, currencyId, chargeAmount, accountItemIds.Count);
        }

        public static bool IsCurrentOrEmptyTarget(IWorldSession session, NetworkIdentity identity)
        {
            if (identity == null || identity.Id == 0ul)
                return true;

            if (session.Player == null)
                return false;

            return identity.Id == session.Player.Identity.Id &&
                (identity.RealmId == 0u || identity.RealmId == session.Player.Identity.RealmId);
        }

        public static NetworkIdentity GetCurrentPlayerIdentity(IWorldSession session)
        {
            if (session.Player == null)
                return new NetworkIdentity();

            return new NetworkIdentity
            {
                RealmId = session.Player.Identity.RealmId,
                Id      = session.Player.Identity.Id
            };
        }

        public static void SendFailure(IWorldSession session, GenericError error)
        {
            session.Player?.SendGenericError(error);
        }

        private static bool TryGetChargeAmount(IOfferItemPrice price, out ulong chargeAmount)
        {
            chargeAmount = 0ul;
            if (float.IsNaN(price.Price) || float.IsInfinity(price.Price) || price.Price < 0f)
                return false;

            double roundedPrice = Math.Ceiling(price.Price);
            if (roundedPrice > ulong.MaxValue)
                return false;

            chargeAmount = (ulong)roundedPrice;
            return true;
        }

        private static bool TryBuildAccountItemList(
            IWorldSession session,
            IOfferItem offerItem,
            out List<uint> accountItemIds,
            out GenericError error,
            out string reason)
        {
            accountItemIds = [];
            error          = GenericError.Ok;
            reason         = string.Empty;

            foreach (IOfferItemData itemData in offerItem.Items)
            {
                if (itemData.Amount == 0u)
                {
                    error  = GenericError.ItemBadStaticData;
                    reason = $"offer item data {itemData.ItemId} has zero amount";
                    return false;
                }

                if (!session.Account.InventoryManager.CanAddItem(itemData.ItemId))
                {
                    error  = GenericError.ItemBadStaticData;
                    reason = $"account item {itemData.ItemId} does not exist";
                    return false;
                }

                if (itemData.Amount > int.MaxValue || accountItemIds.Count > int.MaxValue - (int)itemData.Amount)
                {
                    error  = GenericError.ItemBadStaticData;
                    reason = $"offer account item count overflows for account item {itemData.ItemId}";
                    return false;
                }

                for (uint i = 0u; i < itemData.Amount; i++)
                    accountItemIds.Add(itemData.ItemId);
            }

            if (accountItemIds.Count == 0)
            {
                error  = GenericError.ItemBadStaticData;
                reason = "offer has no account item data";
                return false;
            }

            return true;
        }
    }
}
