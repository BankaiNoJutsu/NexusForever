using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Network;
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
            log.LogDebug("Rejecting CREDD exchange info request from player {PlayerGuid}: reason exchange-service-unavailable.",
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
            if (cancelOrder.OrderId == 0ul)
                throw new InvalidPacketValueException();

            log.LogDebug("Rejecting CREDD exchange cancel request from player {PlayerGuid}: order id {OrderId}, reason exchange-service-unavailable.",
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
            if (buyOrderSubmit.CreditAmount == 0ul)
                throw new InvalidPacketValueException();

            log.LogDebug("Rejecting CREDD exchange buy request from player {PlayerGuid}: credits {CreditAmount}, flag {SubmitFlag}, reason exchange-service-unavailable.",
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
            if (sellOrderSubmit.CreditAmount == 0ul)
                throw new InvalidPacketValueException();

            log.LogDebug("Rejecting CREDD exchange sell request from player {PlayerGuid}: target {TargetIdentity}, credits {CreditAmount}, flag {SubmitFlag}, reason exchange-service-unavailable.",
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
            log.LogDebug("Rejecting CREDD exchange history request from player {PlayerGuid}: reason exchange-service-unavailable.",
                session.Player?.Guid);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.CREDDExchangeNotLoaded);
        }
    }
}
