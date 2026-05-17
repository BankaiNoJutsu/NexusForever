using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.CREDDExchange;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientCREDDExchangeRequestInfoHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeRequestInfo>
    {
        private readonly ILogger<ClientCREDDExchangeRequestInfoHandler> log;

        public ClientCREDDExchangeRequestInfoHandler(ILogger<ClientCREDDExchangeRequestInfoHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeRequestInfo requestInfo)
        {
            log.LogDebug("Rejecting unsupported CREDD exchange info request from player {PlayerGuid}.",
                session.Player?.Guid);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }

    public class ClientCREDDExchangeCancelOrderHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeCancelOrder>
    {
        private readonly ILogger<ClientCREDDExchangeCancelOrderHandler> log;

        public ClientCREDDExchangeCancelOrderHandler(ILogger<ClientCREDDExchangeCancelOrderHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeCancelOrder cancelOrder)
        {
            log.LogDebug("Rejecting unsupported CREDD exchange cancel request from player {PlayerGuid}: order id {OrderId}.",
                session.Player?.Guid, cancelOrder.OrderId);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.CancelCREDDOrder, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }

    public class ClientCREDDExchangeBuyOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeBuyOrderSubmit>
    {
        private readonly ILogger<ClientCREDDExchangeBuyOrderSubmitHandler> log;

        public ClientCREDDExchangeBuyOrderSubmitHandler(ILogger<ClientCREDDExchangeBuyOrderSubmitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeBuyOrderSubmit buyOrderSubmit)
        {
            log.LogDebug("Rejecting unsupported CREDD exchange buy request from player {PlayerGuid}: credits {CreditAmount}, flag {SubmitFlag}.",
                session.Player?.Guid, buyOrderSubmit.CreditAmount, buyOrderSubmit.SubmitFlag);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.BuyCREDD, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }

    public class ClientCREDDExchangeSellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeSellOrderSubmit>
    {
        private readonly ILogger<ClientCREDDExchangeSellOrderSubmitHandler> log;

        public ClientCREDDExchangeSellOrderSubmitHandler(ILogger<ClientCREDDExchangeSellOrderSubmitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeSellOrderSubmit sellOrderSubmit)
        {
            log.LogDebug("Rejecting unsupported CREDD exchange sell request from player {PlayerGuid}: target {TargetIdentity}, credits {CreditAmount}, flag {SubmitFlag}.",
                session.Player?.Guid, sellOrderSubmit.TargetIdentity, sellOrderSubmit.CreditAmount, sellOrderSubmit.SubmitFlag);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.SellCREDD, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }

    public class ClientCREDDExchangeRequestHistoryHandler : IMessageHandler<IWorldSession, ClientCREDDExchangeRequestHistory>
    {
        private readonly ILogger<ClientCREDDExchangeRequestHistoryHandler> log;

        public ClientCREDDExchangeRequestHistoryHandler(ILogger<ClientCREDDExchangeRequestHistoryHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDExchangeRequestHistory requestHistory)
        {
            log.LogDebug("Rejecting unsupported CREDD exchange history request from player {PlayerGuid}.",
                session.Player?.Guid);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }
}
