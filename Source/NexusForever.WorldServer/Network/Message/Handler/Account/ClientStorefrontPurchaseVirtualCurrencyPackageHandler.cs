using Microsoft.Extensions.Logging;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientStorefrontPurchaseVirtualCurrencyPackageHandler
        : IMessageHandler<IWorldSession, ClientStorefrontPurchaseVirtualCurrencyPackage>
    {
        private readonly ILogger<ClientStorefrontPurchaseVirtualCurrencyPackageHandler> log;

        public ClientStorefrontPurchaseVirtualCurrencyPackageHandler(
            ILogger<ClientStorefrontPurchaseVirtualCurrencyPackageHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseVirtualCurrencyPackage purchase)
        {
            if (session.Account == null)
            {
                log.LogWarning("Rejecting virtual-currency package purchase: session has no account.");
                VirtualCurrencyPackageService.SendPurchaseResults(session, isSuccess: false, displayValue: (uint)StoreError.GenericFail);
                return;
            }

            if (!StorePurchaseVelocityLimiter.IsWithinVelocityLimit(session.Account.Id))
            {
                log.LogDebug("Rejecting virtual-currency package purchase for account {AccountId}: velocity limit.",
                    session.Account.Id);
                AccountPrivilegeRestrictionManager.SendStorePurchaseVelocityRestriction(session.Account);
                session.EnqueueMessageEncrypted(new ServerStoreError(StoreError.PurchaseVelocityLimit));
                return;
            }

            if (!VirtualCurrencyPackageService.TryPurchase(session.Account, session, purchase.PackageId, out StoreError error))
            {
                log.LogDebug("Rejecting virtual-currency package purchase for account {AccountId}: package {PackageId}, error {Error}.",
                    session.Account.Id, purchase.PackageId, error);
                session.EnqueueMessageEncrypted(new ServerStoreError(error));
                VirtualCurrencyPackageService.SendPurchaseResults(session, isSuccess: false, displayValue: (uint)error);
                return;
            }

            StorePurchaseHistoryManager.RecordPurchase(session.Account.Id, purchase.PackageId, currencyId: 0, price: 0ul);
            log.LogDebug("Completed virtual-currency package purchase for account {AccountId}: package {PackageId}.",
                session.Account.Id, purchase.PackageId);
        }
    }
}
