using Microsoft.Extensions.Logging;
using NexusForever.Game.Account.Inventory;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientStorefrontRequestPurchaseHistoryHandler
        : IMessageHandler<IWorldSession, ClientStorefrontRequestPurchaseHistory>
    {
        private readonly ILogger<ClientStorefrontRequestPurchaseHistoryHandler> log;

        public ClientStorefrontRequestPurchaseHistoryHandler(
            ILogger<ClientStorefrontRequestPurchaseHistoryHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientStorefrontRequestPurchaseHistory request)
        {
            if (session.Account == null)
            {
                log.LogWarning("Ignoring storefront purchase-history request: session has no account.");
                return;
            }

            log.LogInformation("StorefrontCatalogDiagnostics purchase-history request player={PlayerGuid} account={AccountId}.",
                session.Player?.Guid, session.Account.Id);
            StorePurchaseHistoryManager.SendPurchaseHistory(session.Account);
        }
    }
}
