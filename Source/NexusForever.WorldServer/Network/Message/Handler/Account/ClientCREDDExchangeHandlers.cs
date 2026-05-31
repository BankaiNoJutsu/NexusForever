using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

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
            ServerCREDDExchangeInfoResults infoResults = CREDDExchangeRuntime.BuildInfoResults();
            ServerCREDDExchangeOrderCacheRows orderCacheRows = CREDDExchangeRuntime.BuildOrderCacheRows();

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
            log.LogDebug("Returning CREDD exchange operation history for player {PlayerGuid}.", session.Player?.Guid);
            session.EnqueueMessageEncrypted(CREDDExchangeRuntime.BuildOperationHistory(session));
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.GetCREDDExchangeInfo, AccountOperationResult.Ok);
        }
    }

    internal static class CREDDExchangeRuntime
    {
        private static readonly object syncRoot = new();
        private static readonly List<CREDDOrder> orders = [];
        private static readonly List<CREDDHistoryEntry> operationHistory = [];
        private static ulong nextOrderId = 1ul;
        private static bool loadedFromDatabase;

        private const int MaxHistoryRowsPerAccount = 50;

        public static ServerCREDDExchangeInfoResults BuildInfoResults()
        {
            EnsureLoaded();
            var message = new ServerCREDDExchangeInfoResults();
            lock (syncRoot)
            {
                message.BuyOrderCount  = (uint)orders.Count(o => o.IsBuyOrder);
                message.SellOrderCount = (uint)orders.Count(o => !o.IsBuyOrder);
                message.OwnedOrderCount = (uint)orders.Count(o => o.AccountId != 0u);

                ulong[] buyPrices = orders
                    .Where(o => o.IsBuyOrder)
                    .Select(o => o.CreditAmount)
                    .OrderByDescending(amount => amount)
                    .Take(ServerCREDDExchangeInfoResults.PriceBucketCount)
                    .ToArray();
                ulong[] sellPrices = orders
                    .Where(o => !o.IsBuyOrder)
                    .Select(o => o.CreditAmount)
                    .OrderBy(amount => amount)
                    .Take(ServerCREDDExchangeInfoResults.PriceBucketCount)
                    .ToArray();

                for (int i = 0; i < ServerCREDDExchangeInfoResults.PriceBucketCount; i++)
                {
                    message.BuyOrderPrices[i]  = i < buyPrices.Length ? buyPrices[i] : 0ul;
                    message.SellOrderPrices[i] = i < sellPrices.Length ? sellPrices[i] : 0ul;
                }
            }

            return message;
        }

        public static ServerCREDDExchangeOrderCacheRows BuildOrderCacheRows()
        {
            EnsureLoaded();
            var message = new ServerCREDDExchangeOrderCacheRows();
            lock (syncRoot)
            {
                foreach (CREDDOrder order in orders)
                {
                    message.Rows.Add(new ServerCREDDExchangeOrderCacheRows.Row
                    {
                        OrderId      = order.OrderId,
                        CreditAmount = (uint)Math.Min(order.CreditAmount, (1u << 14) - 1u),
                        SideFlag     = order.IsBuyOrder ? 1u : 0u
                    });
                }
            }

            return message;
        }

        public static ServerCREDDOperationHistory BuildOperationHistory(IWorldSession session)
        {
            var message = new ServerCREDDOperationHistory();
            IAccount account = session?.Account;
            if (account == null)
                return message;

            EnsureLoaded();
            DateTime now = DateTime.UtcNow;
            lock (syncRoot)
            {
                foreach (CREDDHistoryEntry entry in operationHistory
                    .Where(h => h.AccountId == account.Id)
                    .OrderByDescending(h => h.CreatedUtc)
                    .Take(MaxHistoryRowsPerAccount))
                {
                    message.Rows.Add(entry.ToNetworkRow(now));
                }
            }

            if (message.Rows.Count == 0)
            {
                AuthDatabase authDatabase = TryGetAuthDatabase();
                if (authDatabase != null)
                {
                    foreach (AccountCREDDHistoryModel historyModel in authDatabase.GetCREDDHistory(account.Id, MaxHistoryRowsPerAccount))
                        message.Rows.Add(ToNetworkRow(historyModel, now));
                }
            }

            return message;
        }

        public static AccountOperation SubmitBuyOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.BuyCREDD;

            EnsureLoaded();
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
                    RemoveOrder(sellOrder);
                    NetworkIdentity sellerIdentity = seller == null ? sellOrder.Identity : CloneIdentity(seller.Identity, seller.CharacterId);
                    RecordHistory(session.Account.Id, session.Player, AccountOperation.BuyCREDDComplete, sellOrder.CreditAmount, true, sellerIdentity);
                    RecordHistory(sellOrder.AccountId, sellOrder.Identity, AccountOperation.SellCREDDComplete, sellOrder.CreditAmount, false,
                        CloneIdentity(session.Player.Identity, session.Player.CharacterId));

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
                AddOrder(new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    Identity     = CloneIdentity(session.Player.Identity, session.Player.CharacterId),
                    CreditAmount = creditAmount,
                    IsBuyOrder   = true
                });
                RecordHistory(session.Account.Id, session.Player, AccountOperation.BuyCREDD, creditAmount, true);

                result = AccountOperationResult.Ok;
                return AccountOperation.BuyCREDD;
            }
        }

        public static AccountOperation SubmitSellOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.SellCREDD;

            EnsureLoaded();
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
                    RemoveOrder(buyOrder);
                    NetworkIdentity buyerIdentity = buyer == null ? buyOrder.Identity : CloneIdentity(buyer.Identity, buyer.CharacterId);
                    RecordHistory(session.Account.Id, session.Player, AccountOperation.SellCREDDComplete, buyOrder.CreditAmount, true, buyerIdentity);
                    RecordHistory(buyOrder.AccountId, buyOrder.Identity, AccountOperation.BuyCREDDComplete, buyOrder.CreditAmount, false,
                        CloneIdentity(session.Player.Identity, session.Player.CharacterId));

                    result = AccountOperationResult.Ok;
                    return AccountOperation.SellCREDDComplete;
                }

                if (!submitFlag)
                {
                    result = AccountOperationResult.NoMatchingOrder;
                    return AccountOperation.SellCREDD;
                }

                AddOrder(new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    Identity     = CloneIdentity(session.Player.Identity, session.Player.CharacterId),
                    CreditAmount = creditAmount,
                    IsBuyOrder   = false
                });
                RecordHistory(session.Account.Id, session.Player, AccountOperation.SellCREDD, creditAmount, true);

                result = AccountOperationResult.Ok;
                return AccountOperation.SellCREDD;
            }
        }

        public static AccountOperationResult CancelOrder(IWorldSession session, ulong orderId)
        {
            if (session.Player == null)
                return AccountOperationResult.GenericFail;

            EnsureLoaded();
            lock (syncRoot)
            {
                CREDDOrder order = orders.FirstOrDefault(o => o.OrderId == orderId && o.AccountId == session.Account.Id);
                if (order == null)
                    return AccountOperationResult.NoMatchingOrder;

                RemoveOrder(order);
                if (order.IsBuyOrder)
                    session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, order.CreditAmount);
                RecordHistory(session.Account.Id, session.Player, AccountOperation.CancelCREDDOrder, order.CreditAmount, true);

                return AccountOperationResult.Ok;
            }
        }

        private static void RecordHistory(uint accountId, IPlayer player, AccountOperation operation, ulong creditAmount, bool isInitiator, NetworkIdentity counterparty = null)
        {
            RecordHistory(accountId, CloneIdentity(player?.Identity, player?.CharacterId ?? 0ul), operation, creditAmount, isInitiator, counterparty);
        }

        private static void RecordHistory(uint accountId, NetworkIdentity identity, AccountOperation operation, ulong creditAmount, bool isInitiator, NetworkIdentity counterparty = null)
        {
            var entry = new CREDDHistoryEntry
            {
                AccountId     = accountId,
                CreatedUtc    = DateTime.UtcNow,
                Operation     = operation,
                IsInitiator   = isInitiator,
                Identity      = CloneIdentity(identity),
                Counterparty  = CloneIdentity(counterparty),
                CreditAmount  = creditAmount
            };

            operationHistory.Add(entry);
            PersistHistory(entry);

            int overflowRows = operationHistory.Count(h => h.AccountId == accountId) - MaxHistoryRowsPerAccount;
            if (overflowRows <= 0)
                return;

            foreach (CREDDHistoryEntry oldEntry in operationHistory
                .Where(h => h.AccountId == accountId)
                .OrderBy(h => h.CreatedUtc)
                .Take(overflowRows)
                .ToList())
            {
                operationHistory.Remove(oldEntry);
            }
        }

        private static AuthDatabase TryGetAuthDatabase()
        {
            try
            {
                return DatabaseManager.Instance?.GetDatabase<AuthDatabase>();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return null;
            }
        }

        private static void EnsureLoaded()
        {
            if (loadedFromDatabase)
                return;

            lock (syncRoot)
            {
                if (loadedFromDatabase)
                    return;

                AuthDatabase authDatabase = TryGetAuthDatabase();
                if (authDatabase == null)
                {
                    loadedFromDatabase = true;
                    return;
                }

                foreach (AccountCREDDOrderModel model in authDatabase.GetCREDDOrders())
                {
                    orders.Add(new CREDDOrder
                    {
                        OrderId      = model.OrderId,
                        AccountId    = model.AccountId,
                        CharacterId  = model.CharacterId,
                        Identity     = new NetworkIdentity
                        {
                            RealmId = model.RealmId,
                            Id      = model.CharacterId
                        },
                        CreditAmount = model.CreditAmount,
                        IsBuyOrder   = model.IsBuyOrder
                    });
                    nextOrderId = Math.Max(nextOrderId, model.OrderId + 1ul);
                }

                loadedFromDatabase = true;
            }
        }

        private static void AddOrder(CREDDOrder order)
        {
            orders.Add(order);
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase == null)
                return;

            authDatabase.UpsertCREDDOrder(new AccountCREDDOrderModel
            {
                OrderId      = order.OrderId,
                AccountId    = order.AccountId,
                CharacterId  = order.CharacterId,
                RealmId      = order.Identity.RealmId,
                CreditAmount = order.CreditAmount,
                IsBuyOrder   = order.IsBuyOrder
            });
        }

        private static void RemoveOrder(CREDDOrder order)
        {
            orders.Remove(order);
            TryGetAuthDatabase()?.RemoveCREDDOrder(order.OrderId);
        }

        private static void PersistHistory(CREDDHistoryEntry entry)
        {
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase == null)
                return;

            authDatabase.AddCREDDHistory(new AccountCREDDHistoryModel
            {
                AccountId               = entry.AccountId,
                CreatedUtc              = entry.CreatedUtc,
                Operation               = (uint)entry.Operation,
                IsInitiator             = entry.IsInitiator,
                IdentityRealmId         = entry.Identity?.RealmId ?? 0,
                IdentityCharacterId     = entry.Identity?.Id ?? 0ul,
                CounterpartyRealmId     = entry.Counterparty?.RealmId ?? 0,
                CounterpartyCharacterId = entry.Counterparty?.Id ?? 0ul,
                CreditAmount            = entry.CreditAmount
            });
        }

        private static ServerUnresolvedAccountIdentityRowListPayload.Row ToNetworkRow(AccountCREDDHistoryModel model, DateTime now)
        {
            return new ServerUnresolvedAccountIdentityRowListPayload.Row
            {
                Operation         = model.Operation,
                IsInitiator       = model.IsInitiator,
                LogAgeMinutes     = (uint)Math.Max(0d, Math.Floor((now - model.CreatedUtc).TotalMinutes)),
                Identity0         = new NetworkIdentity
                {
                    RealmId = model.IdentityRealmId,
                    Id      = model.IdentityCharacterId
                },
                Identity1         = new NetworkIdentity
                {
                    RealmId = model.CounterpartyRealmId,
                    Id      = model.CounterpartyCharacterId
                },
                FriendCharacterId = model.CounterpartyCharacterId,
                MoneyAmount       = model.CreditAmount
            };
        }

        private static NetworkIdentity CloneIdentity(NetworkIdentity identity, ulong fallbackId = 0ul)
        {
            if (identity != null)
            {
                return new NetworkIdentity
                {
                    RealmId = identity.RealmId,
                    Id      = identity.Id
                };
            }

            return new NetworkIdentity
            {
                RealmId = 0,
                Id      = fallbackId
            };
        }

        private static NetworkIdentity CloneIdentity(GameIdentity identity, ulong fallbackId = 0ul)
        {
            if (identity != null)
            {
                return new NetworkIdentity
                {
                    RealmId = identity.RealmId,
                    Id      = identity.Id
                };
            }

            return CloneIdentity((NetworkIdentity)null, fallbackId);
        }

        private sealed class CREDDOrder
        {
            public ulong OrderId { get; init; }
            public uint AccountId { get; init; }
            public ulong CharacterId { get; init; }
            public NetworkIdentity Identity { get; init; }
            public ulong CreditAmount { get; init; }
            public bool IsBuyOrder { get; init; }
        }

        private sealed class CREDDHistoryEntry
        {
            public uint AccountId { get; init; }
            public DateTime CreatedUtc { get; init; }
            public AccountOperation Operation { get; init; }
            public bool IsInitiator { get; init; }
            public NetworkIdentity Identity { get; init; }
            public NetworkIdentity Counterparty { get; init; }
            public ulong CreditAmount { get; init; }

            public ServerUnresolvedAccountIdentityRowListPayload.Row ToNetworkRow(DateTime now)
            {
                return new ServerUnresolvedAccountIdentityRowListPayload.Row
                {
                    Operation        = (uint)Operation,
                    IsInitiator      = IsInitiator,
                    LogAgeMinutes    = (uint)Math.Max(0d, Math.Floor((now - CreatedUtc).TotalMinutes)),
                    Identity0        = CloneIdentity(Identity),
                    Identity1        = CloneIdentity(Counterparty),
                    FriendCharacterId = Counterparty?.Id ?? 0ul,
                    MoneyAmount      = CreditAmount
                };
            }
        }
    }
}
