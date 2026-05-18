using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
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
            log.LogDebug("Returning CREDD exchange info acknowledgement for player {PlayerGuid}.", session.Player?.Guid);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.Ok);
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

            AccountOperationResult result = CREDDExchangeRuntime.CancelOrder(session, cancelOrder.OrderId);
            log.LogDebug("Processed CREDD exchange cancel request from player {PlayerGuid}: order id {OrderId}, result {Result}.",
                session.Player?.Guid, cancelOrder.OrderId, result);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.CancelCREDDOrder, result);
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

            AccountOperation operation = CREDDExchangeRuntime.SubmitBuyOrder(session, buyOrderSubmit.CreditAmount, buyOrderSubmit.SubmitFlag, out AccountOperationResult result);
            log.LogDebug("Processed CREDD exchange buy request from player {PlayerGuid}: credits {CreditAmount}, flag {SubmitFlag}, operation {Operation}, result {Result}.",
                session.Player?.Guid, buyOrderSubmit.CreditAmount, buyOrderSubmit.SubmitFlag, operation, result);
            ClientAccountItemOperationResultHelper.Send(session, operation, result);
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

            AccountOperation operation = CREDDExchangeRuntime.SubmitSellOrder(session, sellOrderSubmit.CreditAmount, sellOrderSubmit.SubmitFlag, out AccountOperationResult result);
            log.LogDebug("Processed CREDD exchange sell request from player {PlayerGuid}: target {TargetIdentity}, credits {CreditAmount}, flag {SubmitFlag}, operation {Operation}, result {Result}.",
                session.Player?.Guid, sellOrderSubmit.TargetIdentity, sellOrderSubmit.CreditAmount, sellOrderSubmit.SubmitFlag, operation, result);
            ClientAccountItemOperationResultHelper.Send(session, operation, result);
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
            log.LogDebug("Returning CREDD exchange history acknowledgement for player {PlayerGuid}.", session.Player?.Guid);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.Ok);
        }
    }

    internal static class CREDDExchangeRuntime
    {
        private static readonly object syncRoot = new();
        private static readonly List<CREDDOrder> orders = [];
        private static ulong nextOrderId = 1ul;

        public static AccountOperation SubmitBuyOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.BuyCREDD;

            lock (syncRoot)
            {
                CREDDOrder sellOrder = orders
                    .Where(o => !o.IsBuyOrder && o.CreditAmount <= creditAmount && o.AccountId != session.Account.Id)
                    .OrderBy(o => o.CreditAmount)
                    .FirstOrDefault();
                if (sellOrder != null)
                {
                    if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, sellOrder.CreditAmount))
                    {
                        result = AccountOperationResult.NotEnoughCurrency;
                        return AccountOperation.BuyCREDD;
                    }

                    session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, sellOrder.CreditAmount);
                    IPlayer seller = PlayerManager.Instance.GetPlayerByAccountId(sellOrder.AccountId);
                    seller?.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellOrder.CreditAmount);
                    session.Account.EntitlementManager.UpdateEntitlement(EntitlementType.CREDDUsage, 1);
                    orders.Remove(sellOrder);

                    result = AccountOperationResult.Ok;
                    return AccountOperation.BuyCREDDComplete;
                }

                if (!submitFlag)
                {
                    result = AccountOperationResult.NoMatchingOrder;
                    return AccountOperation.BuyCREDD;
                }

                if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, creditAmount))
                {
                    result = AccountOperationResult.NotEnoughCurrency;
                    return AccountOperation.BuyCREDD;
                }

                session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, creditAmount);
                orders.Add(new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    CreditAmount = creditAmount,
                    IsBuyOrder   = true
                });

                result = AccountOperationResult.Ok;
                return AccountOperation.BuyCREDD;
            }
        }

        public static AccountOperation SubmitSellOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.SellCREDD;

            lock (syncRoot)
            {
                CREDDOrder buyOrder = orders
                    .Where(o => o.IsBuyOrder && o.CreditAmount >= creditAmount && o.AccountId != session.Account.Id)
                    .OrderByDescending(o => o.CreditAmount)
                    .FirstOrDefault();
                if (buyOrder != null)
                {
                    IPlayer buyer = PlayerManager.Instance.GetPlayerByAccountId(buyOrder.AccountId);
                    buyer?.Account.EntitlementManager.UpdateEntitlement(EntitlementType.CREDDUsage, 1);
                    session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, buyOrder.CreditAmount);
                    orders.Remove(buyOrder);

                    result = AccountOperationResult.Ok;
                    return AccountOperation.SellCREDDComplete;
                }

                if (!submitFlag)
                {
                    result = AccountOperationResult.NoMatchingOrder;
                    return AccountOperation.SellCREDD;
                }

                orders.Add(new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    CreditAmount = creditAmount,
                    IsBuyOrder   = false
                });

                result = AccountOperationResult.Ok;
                return AccountOperation.SellCREDD;
            }
        }

        public static AccountOperationResult CancelOrder(IWorldSession session, ulong orderId)
        {
            if (session.Player == null)
                return AccountOperationResult.GenericFail;

            lock (syncRoot)
            {
                CREDDOrder order = orders.FirstOrDefault(o => o.OrderId == orderId && o.AccountId == session.Account.Id);
                if (order == null)
                    return AccountOperationResult.NoMatchingOrder;

                orders.Remove(order);
                if (order.IsBuyOrder)
                    session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, order.CreditAmount);

                return AccountOperationResult.Ok;
            }
        }

        private sealed class CREDDOrder
        {
            public ulong OrderId { get; init; }
            public uint AccountId { get; init; }
            public ulong CharacterId { get; init; }
            public ulong CreditAmount { get; init; }
            public bool IsBuyOrder { get; init; }
        }
    }
}
