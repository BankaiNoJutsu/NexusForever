using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NLog;

using NexusForever.Database;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientStorefrontRequestCatalogHandler : IMessageHandler<IWorldSession, ClientStorefrontRequestCatalog>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        #region Dependency Injection

        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IDatabaseManager databaseManager;

        public ClientStorefrontRequestCatalogHandler(
            IGlobalStorefrontManager globalStorefrontManager,
            IDatabaseManager databaseManager = null)
        {
            this.globalStorefrontManager = globalStorefrontManager;
            this.databaseManager = databaseManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientStorefrontRequestCatalog request)
        {
            if (!CanProcessCatalogRequest(session))
            {
                log.Info($"StorefrontCatalogDiagnostics deferring request account={session.Account?.Id ?? 0u} player={session.Player?.Guid ?? 0u} catalogContext={request.CatalogContext} pregameAccountPacketsSent={session.HasSentPregameAccountPackets} characterListPacketsSent={session.HasSentCharacterListPackets} playerLoading={session.Player?.IsLoading ?? false} until pregame account packets are sent and any in-world player has finished loading.");

                session.Events.EnqueueEvent(new PredicateEvent(
                    () => CanProcessCatalogRequest(session),
                    () => SendCatalogResponse(session, request)));

                return;
            }

            SendCatalogResponse(session, request);
        }

        private static bool CanProcessCatalogRequest(IWorldSession session)
        {
            if (!session.HasSentPregameAccountPackets)
                return false;

            if (session.Player != null && session.Player.IsLoading)
                return false;

            return true;
        }

        private void SendCatalogResponse(IWorldSession session, ClientStorefrontRequestCatalog request)
        {
            uint accountId = session.Account.Id;

            log.Info($"StorefrontCatalogDiagnostics request start account={accountId} player={session.Player?.Guid ?? 0u} catalogContext={request.CatalogContext}.");

            session.Account.InventoryManager.SendInitialPackets();
            StorePurchaseHistoryManager.SendPurchaseHistory(session.Account, databaseManager);

            if (session.Player == null)
                globalStorefrontManager.MarkAccountCatalogRequestedBeforeWorldLogin(accountId);

            // Retail ties StoreCatalogReady to the 082D response; deferring catalog to a later event tick
            // left the client in Catalogue Unavailable even though packets were enqueued afterward.
            globalStorefrontManager.HandleCatalogRequest(session, accountId);

            log.Info($"StorefrontCatalogDiagnostics request end account={accountId} player={session.Player?.Guid ?? 0u} catalogContext={request.CatalogContext} (catalog sent).");
        }
    }
}
