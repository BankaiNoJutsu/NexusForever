using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Account.Inventory;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneNotifyStorefrontHandler : IMessageHandler<IWorldSession, ClientFortuneNotifyStorefront>
    {
        private readonly ILogger<ClientFortuneNotifyStorefrontHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IFortuneSessionManager fortuneSessionManager;

        public ClientFortuneNotifyStorefrontHandler(
            ILogger<ClientFortuneNotifyStorefrontHandler> log,
            IGlobalStorefrontManager globalStorefrontManager,
            IFortuneSessionManager fortuneSessionManager)
        {
            this.log                     = log;
            this.globalStorefrontManager = globalStorefrontManager;
            this.fortuneSessionManager   = fortuneSessionManager;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneNotifyStorefront _)
        {
            log.LogDebug("ClientFortuneNotifyStorefront: player={Player}", session.Player?.Guid);
            if (session.Account != null)
            {
                log.LogInformation("StorefrontCatalogDiagnostics storefront notify request player={PlayerGuid} account={AccountId}; sending account inventory, purchase history, and catalog before fortune status.",
                    session.Player?.Guid, session.Account.Id);

                session.Account.InventoryManager.SendInitialPackets();
                StorePurchaseHistoryManager.SendPurchaseHistory(session.Account);
                globalStorefrontManager.HandleCatalogRequest(session, session.Account.Id);
            }

            fortuneSessionManager.SendStatus(session);
        }
    }
}
