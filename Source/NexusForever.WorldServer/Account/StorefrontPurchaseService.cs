using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Storefront;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Account
{
    public sealed class StorefrontPurchaseService : IStorefrontPurchaseService
    {
        private readonly IDatabaseManager databaseManager;
        private readonly IGlobalStorefrontManager globalStorefrontManager;

        private const int AccountLockStripeCount = 256;

        private static readonly object[] accountLocks = CreateAccountLockStripes();

        public StorefrontPurchaseService(
            IDatabaseManager databaseManager,
            IGlobalStorefrontManager globalStorefrontManager)
        {
            this.databaseManager          = databaseManager;
            this.globalStorefrontManager = globalStorefrontManager;
        }

        private static object[] CreateAccountLockStripes()
        {
            var stripes = new object[AccountLockStripeCount];
            for (int i = 0; i < stripes.Length; i++)
                stripes[i] = new object();

            return stripes;
        }

        private static object GetAccountLock(uint accountId) => accountLocks[accountId % AccountLockStripeCount];

        public void TryPurchase(
            IWorldSession session,
            ILogger log,
            uint offerId,
            byte paymentCurrencySlot,
            ushort currencyId,
            Action<IOfferItem, IReadOnlyList<uint>> deliverItems,
            Action<IWorldSession> sendSuccess,
            string purchaseScope,
            StorefrontDeliveryValidator deliveryValidator = null,
            bool requirePlayer = true,
            Action rollbackDelivery = null)
        {
            log.LogInformation("StorefrontCatalogDiagnostics storefront {PurchaseScope} purchase validate player={PlayerGuid} account={AccountId} offer={OfferId} paymentCurrencySlot={PaymentCurrencySlot} currency={CurrencyId}.",
                purchaseScope, session.Player?.Guid, session.Account?.Id, offerId, paymentCurrencySlot, currencyId);

            if (requirePlayer && session.Player == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase for offer {OfferId}: session has no player.",
                    purchaseScope, offerId);
                SendFailure(session, StoreError.GenericFail);
                return;
            }

            if (session.Account == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase for offer {OfferId}: session has no account.",
                    purchaseScope, offerId);
                SendFailure(session, StoreError.GenericFail);
                return;
            }

            lock (GetAccountLock(session.Account.Id))
            {
                TryPurchaseLocked(
                    session,
                    log,
                    offerId,
                    paymentCurrencySlot,
                    currencyId,
                    deliverItems,
                    sendSuccess,
                    purchaseScope,
                    deliveryValidator,
                    rollbackDelivery);
            }
        }

        private void TryPurchaseLocked(
            IWorldSession session,
            ILogger log,
            uint offerId,
            byte paymentCurrencySlot,
            ushort currencyId,
            Action<IOfferItem, IReadOnlyList<uint>> deliverItems,
            Action<IWorldSession> sendSuccess,
            string purchaseScope,
            StorefrontDeliveryValidator deliveryValidator,
            Action rollbackDelivery)
        {
            IOfferItem offerItem = globalStorefrontManager.GetStoreOfferItem(offerId);
            if (offerItem == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: unknown offer {OfferId}.",
                    purchaseScope, session.Player?.Guid, offerId);
                SendFailure(session, StoreError.InvalidOffer);
                return;
            }

            if (!TryResolvePurchaseCurrency(paymentCurrencySlot, currencyId, out AccountCurrencyType accountCurrencyType, out ushort resolvedCurrencyId))
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: invalid payment currency slot {PaymentCurrencySlot} and currency {CurrencyId} for offer {OfferId}.",
                    purchaseScope, session.Player?.Guid, paymentCurrencySlot, currencyId, offerId);
                SendFailure(session, StoreError.InvalidPrice);
                return;
            }

            IOfferItemPrice price = offerItem.GetPriceDataForCurrency(accountCurrencyType);
            if (price == null)
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} has no price for resolved currency {ResolvedCurrencyId} (slot {PaymentCurrencySlot}, currency {CurrencyId}).",
                    purchaseScope, session.Player?.Guid, offerId, resolvedCurrencyId, paymentCurrencySlot, currencyId);
                SendFailure(session, StoreError.InvalidPrice);
                return;
            }

            if (!TryGetChargeAmount(price, out ulong chargeAmount))
            {
                log.LogWarning("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} has invalid price {Price}.",
                    purchaseScope, session.Player?.Guid, offerId, price.Price);
                SendFailure(session, StoreError.InvalidPrice);
                return;
            }

            if (!TryBuildAccountItemList(session, offerItem, out List<uint> accountItemIds, out StoreError error, out string reason))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} cannot be placed in account inventory ({Reason}).",
                    purchaseScope, session.Player?.Guid, offerId, reason);
                SendFailure(session, error);
                return;
            }

            if (deliveryValidator != null && !deliveryValidator(offerItem, accountItemIds, out error, out reason))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: offer {OfferId} cannot be delivered ({Reason}).",
                    purchaseScope, session.Player?.Guid, offerId, reason);
                SendFailure(session, error);
                return;
            }

            if (chargeAmount > 0ul && !session.Account.CurrencyManager.CanAfford(accountCurrencyType, chargeAmount))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: insufficient currency {CurrencyId}, price {Price}.",
                    purchaseScope, session.Player?.Guid, resolvedCurrencyId, chargeAmount);
                SendFailure(session, StoreError.CannotUseOffer);
                return;
            }

            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (!StorePurchaseHistoryManager.IsWithinVelocityLimit(session.Account.Id, authDatabase))
            {
                log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: purchase velocity limit reached for account {AccountId}.",
                    purchaseScope, session.Player?.Guid, session.Account.Id);
                SendFailure(session, StoreError.PurchaseVelocityLimit);
                return;
            }

            bool historyReserved = false;
            ulong reservedHistoryId = 0ul;
            if (authDatabase != null)
            {
                historyReserved = true;
                if (!StorePurchaseHistoryManager.TryRecordPurchase(authDatabase, session.Account.Id, offerId, resolvedCurrencyId, chargeAmount, out reservedHistoryId))
                {
                    log.LogDebug("Rejecting storefront {PurchaseScope} purchase from player {PlayerGuid}: purchase velocity limit reached for account {AccountId} before delivery.",
                        purchaseScope, session.Player?.Guid, session.Account.Id);
                    SendFailure(session, StoreError.PurchaseVelocityLimit);
                    return;
                }
            }

            bool chargeDebited = false;
            try
            {
                if (chargeAmount > 0ul)
                {
                    session.Account.CurrencyManager.CurrencySubtractAmount(accountCurrencyType, chargeAmount);
                    chargeDebited = true;
                }

                deliverItems(offerItem, accountItemIds);
            }
            catch (Exception ex)
            {
                TryRollbackDelivery(rollbackDelivery, log, purchaseScope, session, offerId);
                TryRefundCharge(session, log, accountCurrencyType, chargeAmount, chargeDebited, purchaseScope, offerId);

                if (historyReserved)
                    StorePurchaseHistoryManager.TryDeletePurchaseHistory(authDatabase, reservedHistoryId);

                log.LogWarning(ex, "StorefrontCatalogDiagnostics storefront {PurchaseScope} purchase failed during delivery player={PlayerGuid} account={AccountId} offer={OfferId}.",
                    purchaseScope, session.Player?.Guid, session.Account.Id, offerId);
                SendFailure(session, StoreError.GenericFail);
                return;
            }

            if (!TryPersistAccount(session, log))
            {
                TryRollbackDelivery(rollbackDelivery, log, purchaseScope, session, offerId);
                TryRefundCharge(session, log, accountCurrencyType, chargeAmount, chargeDebited, purchaseScope, offerId);

                if (historyReserved)
                    StorePurchaseHistoryManager.TryDeletePurchaseHistory(authDatabase, reservedHistoryId);

                SendFailure(session, StoreError.GenericFail);
                return;
            }

            if (!historyReserved && !StorePurchaseHistoryManager.TryRecordPurchase(authDatabase, session.Account.Id, offerId, resolvedCurrencyId, chargeAmount))
            {
                TryRollbackDelivery(rollbackDelivery, log, purchaseScope, session, offerId);
                TryRefundCharge(session, log, accountCurrencyType, chargeAmount, chargeDebited, purchaseScope, offerId);
                SendFailure(session, StoreError.PurchaseVelocityLimit);
                return;
            }

            sendSuccess(session);

            log.LogInformation("StorefrontCatalogDiagnostics storefront {PurchaseScope} purchase completed player={PlayerGuid} account={AccountId} offer={OfferId} currency={CurrencyId} price={Price} accountItems={AccountItemCount}.",
                purchaseScope, session.Player?.Guid, session.Account.Id, offerId, resolvedCurrencyId, chargeAmount, accountItemIds.Count);

            log.LogDebug("Completed storefront {PurchaseScope} purchase for player {PlayerGuid}: offer {OfferId}, currency {CurrencyId}, price {Price}, account items {AccountItemCount}.",
                purchaseScope, session.Player?.Guid, offerId, resolvedCurrencyId, chargeAmount, accountItemIds.Count);
        }

        public bool IsCurrentOrEmptyTarget(IWorldSession session, NetworkIdentity identity)
        {
            if (identity == null || identity.Id == 0ul)
                return true;

            if (session.Player == null)
                return false;

            return identity.Id == session.Player.Identity.Id &&
                (identity.RealmId == 0u || identity.RealmId == session.Player.Identity.RealmId);
        }

        public NetworkIdentity GetCurrentPlayerIdentity(IWorldSession session)
        {
            if (session.Player == null)
                return new NetworkIdentity();

            return new NetworkIdentity
            {
                RealmId = session.Player.Identity.RealmId,
                Id      = session.Player.Identity.Id
            };
        }

        public void SendFailure(IWorldSession session, StoreError error)
        {
            if (error == StoreError.PurchaseVelocityLimit)
                AccountPrivilegeRestrictionManager.SendStorePurchaseVelocityRestriction(session.Account);

            session.EnqueueMessageEncrypted(new ServerStoreError(error));
        }

        /// <summary>
        /// Character purchases use opcode 0x098C. Native client handlers route 0x098C and 0x098D
        /// through the same purchase-result path, so account purchases emit both to satisfy
        /// character-select flows that keep the processing overlay open after variant-only results.
        /// </summary>
        public void SendCharacterPurchaseSuccess(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerStorePurchaseOfferResult
            {
                IsSuccess   = true,
                DisplayType = PurchaseResultDisplayType.Default
            });
        }

        public void SendAccountPurchaseSuccess(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerStorePurchaseOfferResultVariant
            {
                IsSuccess   = true,
                DisplayType = PurchaseResultDisplayType.Default
            });
            session.EnqueueMessageEncrypted(new ServerStorePurchaseOfferResult
            {
                IsSuccess   = true,
                DisplayType = PurchaseResultDisplayType.Default
            });
        }

        public bool ValidateDirectAccountGrantClaim(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IOfferItem offerItem,
            out StoreError error,
            out string reason)
        {
            error  = StoreError.GenericFail;
            reason = string.Empty;

            if (!IsDirectAccountGrantOffer(offerItem))
                return true;

            if (!TryBuildDirectAccountGrantPlan(session, gameTableManager, offerItem, requireDirectAccountGrant: false, out _, out error, out reason))
                return false;

            return true;
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

        private static bool TryResolvePurchaseCurrency(byte paymentCurrencySlot, ushort currencyId, out AccountCurrencyType accountCurrencyType, out ushort resolvedCurrencyId)
        {
            resolvedCurrencyId = paymentCurrencySlot != 0
                ? paymentCurrencySlot
                : currencyId;
            accountCurrencyType = (AccountCurrencyType)resolvedCurrencyId;

            return Enum.IsDefined(typeof(AccountCurrencyType), accountCurrencyType);
        }

        private static bool IsDirectAccountGrantOffer(IOfferItem offerItem)
        {
            return offerItem.Items.All(itemData => IsDirectAccountGrantItem(itemData.Entry));
        }

        private static bool IsDirectAccountGrantItem(AccountItemEntry entry)
        {
            if (entry == null)
                return false;

            if (entry.Item2Id != 0u || entry.GenericUnlockSetId != 0u || entry.InstantEventEnum != 0u || entry.AccountItemCooldownGroupId != 0u)
                return false;

            return entry.AccountCurrencyEnum != 0u || entry.EntitlementId != 0u;
        }

        public bool TryBuildDirectAccountGrantPlan(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IOfferItem offerItem,
            bool requireDirectAccountGrant,
            out DirectAccountGrantPlan plan,
            out StoreError error,
            out string reason)
        {
            plan   = new DirectAccountGrantPlan();
            error  = StoreError.GenericFail;
            reason = string.Empty;

            if (requireDirectAccountGrant && !IsDirectAccountGrantOffer(offerItem))
            {
                error  = StoreError.CannotUseOffer;
                reason = "offer is not a direct account grant";
                return false;
            }

            foreach (IOfferItemData itemData in offerItem.Items)
            {
                AccountItemEntry entry = itemData.Entry;
                if (!IsDirectAccountGrantItem(entry))
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"account item {itemData.ItemId} is not a direct account grant";
                    return false;
                }

                if (itemData.Amount == 0u)
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"offer item data {itemData.ItemId} has zero amount";
                    return false;
                }

                if (entry.AccountCurrencyEnum != 0u && !TryAddDirectCurrencyGrant(itemData, entry, plan, out reason))
                {
                    error = StoreError.CannotUseOffer;
                    return false;
                }

                if (entry.EntitlementId != 0u && !TryAddDirectEntitlementGrant(session, gameTableManager, itemData, entry, plan, out error, out reason))
                    return false;
            }

            if (plan.CurrencyGrants.Count == 0 && plan.EntitlementGrants.Count == 0)
            {
                error  = StoreError.CannotUseOffer;
                reason = "offer has no direct account grants";
                return false;
            }

            return true;
        }

        public void ApplyDirectAccountGrantPlan(IWorldSession session, DirectAccountGrantPlan plan)
        {
            var appliedCurrencyGrants = new List<(AccountCurrencyType CurrencyType, ulong Amount)>();
            var appliedEntitlementGrants = new List<(EntitlementType EntitlementType, int Amount)>();

            try
            {
                foreach ((AccountCurrencyType currencyType, ulong amount) in plan.CurrencyGrants)
                {
                    session.Account.CurrencyManager.CurrencyAddAmount(currencyType, amount);
                    appliedCurrencyGrants.Add((currencyType, amount));
                }

                foreach ((EntitlementType entitlementType, int amount) in plan.EntitlementGrants)
                {
                    session.Account.EntitlementManager.UpdateEntitlement(entitlementType, amount);
                    appliedEntitlementGrants.Add((entitlementType, amount));
                }
            }
            catch
            {
                RollbackDirectAccountGrantPlan(session, appliedCurrencyGrants, appliedEntitlementGrants);
                throw;
            }
        }

        public void RollbackDirectAccountGrantPlan(IWorldSession session, DirectAccountGrantPlan plan)
        {
            RollbackDirectAccountGrantPlan(
                session,
                plan.CurrencyGrants.Select(g => (g.Key, g.Value)),
                plan.EntitlementGrants.Select(g => (g.Key, g.Value)));
        }

        private static void RollbackDirectAccountGrantPlan(
            IWorldSession session,
            IEnumerable<(AccountCurrencyType CurrencyType, ulong Amount)> currencyGrants,
            IEnumerable<(EntitlementType EntitlementType, int Amount)> entitlementGrants)
        {
            foreach ((EntitlementType entitlementType, int amount) in entitlementGrants.Reverse())
                session.Account.EntitlementManager.UpdateEntitlement(entitlementType, -amount);

            foreach ((AccountCurrencyType currencyType, ulong amount) in currencyGrants.Reverse())
                session.Account.CurrencyManager.CurrencySubtractAmount(currencyType, amount);
        }

        private static bool TryAddDirectCurrencyGrant(
            IOfferItemData itemData,
            AccountItemEntry entry,
            DirectAccountGrantPlan plan,
            out string reason)
        {
            reason = string.Empty;

            var currencyType = (AccountCurrencyType)entry.AccountCurrencyEnum;
            if (!Enum.IsDefined(typeof(AccountCurrencyType), currencyType) || entry.AccountCurrencyAmount == 0ul)
            {
                reason = $"account item {itemData.ItemId} has invalid account currency grant";
                return false;
            }

            if (itemData.Amount > 1u && entry.AccountCurrencyAmount > ulong.MaxValue / itemData.Amount)
            {
                reason = $"account item {itemData.ItemId} account currency grant overflows";
                return false;
            }

            ulong amount = entry.AccountCurrencyAmount * itemData.Amount;
            if (plan.CurrencyGrants.TryGetValue(currencyType, out ulong pendingAmount))
            {
                if (ulong.MaxValue - pendingAmount < amount)
                {
                    reason = $"account item {itemData.ItemId} account currency grant overflows";
                    return false;
                }

                plan.CurrencyGrants[currencyType] = pendingAmount + amount;
            }
            else
                plan.CurrencyGrants.Add(currencyType, amount);

            return true;
        }

        private static bool TryAddDirectEntitlementGrant(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IOfferItemData itemData,
            AccountItemEntry entry,
            DirectAccountGrantPlan plan,
            out StoreError error,
            out string reason)
        {
            error  = StoreError.GenericFail;
            reason = string.Empty;

            EntitlementEntry entitlementEntry = gameTableManager?.Entitlement?.GetEntry(entry.EntitlementId);
            if (entitlementEntry == null)
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account item {itemData.ItemId} has invalid entitlement {entry.EntitlementId}";
                return false;
            }

            var entitlementFlags = (EntitlementFlags)entitlementEntry.Flags;
            if (entitlementFlags.HasFlag(EntitlementFlags.Disabled) || entitlementFlags.HasFlag(EntitlementFlags.Character))
            {
                error  = StoreError.MissingEntitlement;
                reason = $"account item {itemData.ItemId} entitlement {entry.EntitlementId} is not account scoped";
                return false;
            }

            if (entry.EntitlementCount == 0u || entry.EntitlementCount > int.MaxValue)
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account item {itemData.ItemId} has invalid entitlement count";
                return false;
            }

            ulong grantAmount = (ulong)entry.EntitlementCount * itemData.Amount;
            if (grantAmount > int.MaxValue)
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account item {itemData.ItemId} entitlement grant exceeds supported amount";
                return false;
            }

            var entitlementType = (EntitlementType)entitlementEntry.Id;
            if (!Enum.IsDefined(typeof(EntitlementType), entitlementType))
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account item {itemData.ItemId} has unmapped entitlement {entry.EntitlementId}";
                return false;
            }

            uint currentAmount = session.Account.EntitlementManager.GetEntitlement(entitlementType)?.Amount ?? 0u;
            ulong pendingAmount = plan.EntitlementGrants.TryGetValue(entitlementType, out int pending)
                ? (ulong)pending
                : 0ul;
            if (pendingAmount + grantAmount > int.MaxValue)
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account item {itemData.ItemId} entitlement grant exceeds supported amount";
                return false;
            }

            if (currentAmount + pendingAmount + grantAmount > entitlementEntry.MaxCount)
            {
                error  = StoreError.CannotUseOffer;
                reason = $"account entitlement {entry.EntitlementId} would exceed max count {entitlementEntry.MaxCount}";
                return false;
            }

            if (pendingAmount == 0ul)
                plan.EntitlementGrants.Add(entitlementType, (int)grantAmount);
            else
                plan.EntitlementGrants[entitlementType] = (int)(pendingAmount + grantAmount);

            return true;
        }

        private static bool TryBuildAccountItemList(
            IWorldSession session,
            IOfferItem offerItem,
            out List<uint> accountItemIds,
            out StoreError error,
            out string reason)
        {
            accountItemIds = [];
            error          = StoreError.GenericFail;
            reason         = string.Empty;

            foreach (IOfferItemData itemData in offerItem.Items)
            {
                if (itemData.Amount == 0u)
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"offer item data {itemData.ItemId} has zero amount";
                    return false;
                }

                if (itemData.Type != 0u)
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"offer item data {itemData.ItemId} has unsupported type {itemData.Type}";
                    return false;
                }

                if (!session.Account.InventoryManager.CanAddItem(itemData.ItemId))
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"account item {itemData.ItemId} does not exist";
                    return false;
                }

                if (itemData.Amount > int.MaxValue || accountItemIds.Count > int.MaxValue - (int)itemData.Amount)
                {
                    error  = StoreError.CannotUseOffer;
                    reason = $"offer account item count overflows for account item {itemData.ItemId}";
                    return false;
                }

                for (uint i = 0u; i < itemData.Amount; i++)
                    accountItemIds.Add(itemData.ItemId);
            }

            if (accountItemIds.Count == 0)
            {
                error  = StoreError.CannotUseOffer;
                reason = "offer has no account item data";
                return false;
            }

            return true;
        }

        public void PersistAccount(IWorldSession session, ILogger log)
        {
            TryPersistAccount(session, log);
        }

        private static void TryRollbackDelivery(Action rollbackDelivery, ILogger log, string purchaseScope, IWorldSession session, uint offerId)
        {
            if (rollbackDelivery == null)
                return;

            try
            {
                rollbackDelivery();
            }
            catch (Exception rollbackException)
            {
                log.LogWarning(rollbackException, "StorefrontCatalogDiagnostics storefront {PurchaseScope} rollback failed player={PlayerGuid} account={AccountId} offer={OfferId}.",
                    purchaseScope, session.Player?.Guid, session.Account?.Id, offerId);
            }
        }

        private static void TryRefundCharge(
            IWorldSession session,
            ILogger log,
            AccountCurrencyType accountCurrencyType,
            ulong chargeAmount,
            bool chargeDebited,
            string purchaseScope,
            uint offerId)
        {
            if (!chargeDebited || chargeAmount == 0ul)
                return;

            try
            {
                session.Account.CurrencyManager.CurrencyAddAmount(accountCurrencyType, chargeAmount);
            }
            catch (Exception refundException)
            {
                log.LogWarning(refundException, "StorefrontCatalogDiagnostics storefront {PurchaseScope} refund failed player={PlayerGuid} account={AccountId} offer={OfferId} currency={CurrencyId} amount={Amount}.",
                    purchaseScope, session.Player?.Guid, session.Account?.Id, offerId, (ushort)accountCurrencyType, chargeAmount);
            }
        }

        private bool TryPersistAccount(IWorldSession session, ILogger log)
        {
            if (session?.Account == null)
                return false;

            AuthDatabase authDatabase = TryGetAuthDatabase();

            if (authDatabase == null)
                return true;

            try
            {
                lock (GetAccountLock(session.Account.Id))
                    authDatabase.SaveBlocking(session.Account.Save);

                return true;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "StorefrontCatalogDiagnostics account {AccountId}: failed to persist account state after storefront purchase.",
                    session.Account?.Id);
                return false;
            }
        }

        private AuthDatabase TryGetAuthDatabase()
        {
            try
            {
                return databaseManager?.GetDatabase<AuthDatabase>();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return null;
            }
        }
    }
}
