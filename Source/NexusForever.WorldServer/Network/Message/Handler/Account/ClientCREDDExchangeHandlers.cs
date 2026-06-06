using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.WorldServer.Account;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientCREDDExchangeRequestInfoHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeRequestInfo>
    {
        private readonly ILogger<ClientCREDDExchangeRequestInfoHandler> log;
        private readonly ICREDDExchangeService creddExchangeService;

        public ClientCREDDExchangeRequestInfoHandler(
            ILogger<ClientCREDDExchangeRequestInfoHandler> log,
            ICREDDExchangeService creddExchangeService)
        {
            this.log                  = log;
            this.creddExchangeService = creddExchangeService;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeRequestInfo requestInfo)
        {
            ServerCREDDExchangeInfoResults infoResults = creddExchangeService.BuildInfoResults();
            ServerCREDDExchangeOrderCacheRows orderCacheRows = creddExchangeService.BuildOrderCacheRows();

            log.LogInformation("StorefrontCatalogDiagnostics CREDD info request player={PlayerGuid} account={AccountId} buyOrders={BuyOrderCount} sellOrders={SellOrderCount} ownedOrders={OwnedOrderCount} cacheRows={CacheRowCount}.",
                session.Player?.Guid, session.Account?.Id, infoResults.BuyOrderCount, infoResults.SellOrderCount, infoResults.OwnedOrderCount, orderCacheRows.Rows.Count);

            session.EnqueueMessageEncrypted(infoResults);
            if (orderCacheRows.Rows.Count == 0)
            {
                log.LogInformation("StorefrontCatalogDiagnostics CREDD info request player={PlayerGuid} account={AccountId}: skipping empty ServerCREDDExchangeOrderCacheRows.",
                    session.Player?.Guid, session.Account?.Id);
            }
            else
            {
                session.EnqueueMessageEncrypted(orderCacheRows);
            }

            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.Ok);
        }
    }

    public class ClientCREDDExchangeCancelOrderHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeCancelOrder>
    {
        private readonly ILogger<ClientCREDDExchangeCancelOrderHandler> log;
        private readonly ICREDDExchangeService creddExchangeService;

        public ClientCREDDExchangeCancelOrderHandler(
            ILogger<ClientCREDDExchangeCancelOrderHandler> log,
            ICREDDExchangeService creddExchangeService)
        {
            this.log                  = log;
            this.creddExchangeService = creddExchangeService;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeCancelOrder cancelOrder)
        {
            if (cancelOrder.OrderId == 0ul)
                throw new InvalidPacketValueException();

            AccountOperationResult result = creddExchangeService.CancelOrder(session, cancelOrder.OrderId);
            log.LogDebug("Processed CREDD exchange cancel request from player {PlayerGuid}: order id {OrderId}, result {Result}.",
                session.Player?.Guid, cancelOrder.OrderId, result);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.CancelCREDDOrder, result);
        }
    }

    public class ClientCREDDExchangeBuyOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeBuyOrderSubmit>
    {
        private readonly ILogger<ClientCREDDExchangeBuyOrderSubmitHandler> log;
        private readonly ICREDDExchangeService creddExchangeService;

        public ClientCREDDExchangeBuyOrderSubmitHandler(
            ILogger<ClientCREDDExchangeBuyOrderSubmitHandler> log,
            ICREDDExchangeService creddExchangeService)
        {
            this.log                  = log;
            this.creddExchangeService = creddExchangeService;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeBuyOrderSubmit buyOrderSubmit)
        {
            if (buyOrderSubmit.CreditAmount == 0ul)
                throw new InvalidPacketValueException();

            AccountOperation operation = creddExchangeService.SubmitBuyOrder(session, buyOrderSubmit.CreditAmount, buyOrderSubmit.SubmitFlag, out AccountOperationResult result);
            log.LogDebug("Processed CREDD exchange buy request from player {PlayerGuid}: credits {CreditAmount}, flag {SubmitFlag}, operation {Operation}, result {Result}.",
                session.Player?.Guid, buyOrderSubmit.CreditAmount, buyOrderSubmit.SubmitFlag, operation, result);
            ClientAccountItemOperationResultHelper.Send(session, operation, result);
        }
    }

    public class ClientCREDDExchangeSellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeSellOrderSubmit>
    {
        private readonly ILogger<ClientCREDDExchangeSellOrderSubmitHandler> log;
        private readonly ICREDDExchangeService creddExchangeService;

        public ClientCREDDExchangeSellOrderSubmitHandler(
            ILogger<ClientCREDDExchangeSellOrderSubmitHandler> log,
            ICREDDExchangeService creddExchangeService)
        {
            this.log                  = log;
            this.creddExchangeService = creddExchangeService;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeSellOrderSubmit sellOrderSubmit)
        {
            if (sellOrderSubmit.CreditAmount == 0ul)
                throw new InvalidPacketValueException();

            AccountOperation operation = creddExchangeService.SubmitSellOrder(session, sellOrderSubmit.CreditAmount, sellOrderSubmit.SubmitFlag, out AccountOperationResult result);
            log.LogDebug("Processed CREDD exchange sell request from player {PlayerGuid}: target {TargetIdentity}, credits {CreditAmount}, flag {SubmitFlag}, operation {Operation}, result {Result}.",
                session.Player?.Guid, sellOrderSubmit.TargetIdentity, sellOrderSubmit.CreditAmount, sellOrderSubmit.SubmitFlag, operation, result);
            ClientAccountItemOperationResultHelper.Send(session, operation, result);
        }
    }

    public class ClientCREDDExchangeRequestHistoryHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeRequestHistory>
    {
        private readonly ILogger<ClientCREDDExchangeRequestHistoryHandler> log;
        private readonly ICREDDExchangeService creddExchangeService;

        public ClientCREDDExchangeRequestHistoryHandler(
            ILogger<ClientCREDDExchangeRequestHistoryHandler> log,
            ICREDDExchangeService creddExchangeService)
        {
            this.log                  = log;
            this.creddExchangeService = creddExchangeService;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeRequestHistory requestHistory)
        {
            log.LogDebug("Returning CREDD exchange operation history for player {PlayerGuid}.", session.Player?.Guid);
            session.EnqueueMessageEncrypted(creddExchangeService.BuildOperationHistory(session));
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.Ok);
        }
    }
}
