using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.WorldServer.Network;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Account
{
    public sealed class CREDDExchangeService : ICREDDExchangeService
    {
        private readonly IDatabaseManager databaseManager;
        private readonly IPlayerManager playerManager;

        private readonly object syncRoot = new();
        private readonly List<CREDDOrder> orders = [];
        private readonly List<CREDDHistoryEntry> operationHistory = [];
        private ulong nextOrderId = 1ul;
        private bool loadedFromDatabase;

        private const int MaxHistoryRowsPerAccount = 50;

        public CREDDExchangeService(
            IDatabaseManager databaseManager,
            IPlayerManager playerManager)
        {
            this.databaseManager = databaseManager;
            this.playerManager   = playerManager;
        }

        public ServerCREDDExchangeInfoResults BuildInfoResults()
        {
            EnsureLoaded();
            var message = new ServerCREDDExchangeInfoResults();
            lock (syncRoot)
            {
                message.BuyOrderCount   = (uint)orders.Count(o => o.IsBuyOrder);
                message.SellOrderCount  = (uint)orders.Count(o => !o.IsBuyOrder);
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

        public ServerCREDDExchangeOrderCacheRows BuildOrderCacheRows()
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

        public ServerCREDDOperationHistory BuildOperationHistory(IWorldSession session)
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

        public AccountOperation SubmitBuyOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.BuyCREDD;

            EnsureLoaded();

            CREDDOrder sellOrder;
            lock (syncRoot)
            {
                sellOrder = orders
                    .Where(o => !o.IsBuyOrder && o.CreditAmount <= creditAmount && o.AccountId != session.Account.Id)
                    .OrderBy(o => o.CreditAmount)
                    .FirstOrDefault();
            }

            if (sellOrder != null)
            {
                if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, sellOrder.CreditAmount))
                {
                    result = AccountOperationResult.NotEnoughCurrency;
                    return AccountOperation.BuyCREDD;
                }

                if (!TryReserveRemoveOrder(sellOrder))
                {
                    result = AccountOperationResult.GenericFail;
                    return AccountOperation.BuyCREDD;
                }

                session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, sellOrder.CreditAmount);
                IPlayer seller = playerManager.GetPlayerByAccountId(sellOrder.AccountId);
                CreditCharacterCurrency(sellOrder.CharacterId, sellOrder.CreditAmount, seller);
                session.Account.EntitlementManager.UpdateEntitlement(EntitlementType.CREDDUsage, 1);
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

            CREDDOrder restingBuyOrder;
            lock (syncRoot)
            {
                restingBuyOrder = new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    Identity     = CloneIdentity(session.Player.Identity, session.Player.CharacterId),
                    CreditAmount = creditAmount,
                    IsBuyOrder   = true
                };
            }

            session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, creditAmount);
            if (!TryCommitAddOrder(restingBuyOrder))
            {
                session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, creditAmount);
                lock (syncRoot)
                {
                    if (nextOrderId == restingBuyOrder.OrderId + 1ul)
                        nextOrderId--;
                }

                result = AccountOperationResult.GenericFail;
                return AccountOperation.BuyCREDD;
            }

            RecordHistory(session.Account.Id, session.Player, AccountOperation.BuyCREDD, creditAmount, true);

            result = AccountOperationResult.Ok;
            return AccountOperation.BuyCREDD;
        }

        public AccountOperation SubmitSellOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result)
        {
            result = AccountOperationResult.GenericFail;
            if (session.Player == null)
                return AccountOperation.SellCREDD;

            EnsureLoaded();

            CREDDOrder buyOrder;
            lock (syncRoot)
            {
                buyOrder = orders
                    .Where(o => o.IsBuyOrder && o.CreditAmount >= creditAmount && o.AccountId != session.Account.Id)
                    .OrderByDescending(o => o.CreditAmount)
                    .FirstOrDefault();
            }

            if (buyOrder != null)
            {
                ulong matchedCreditAmount = buyOrder.CreditAmount;
                if (!TryReserveRemoveOrder(buyOrder))
                {
                    result = AccountOperationResult.GenericFail;
                    return AccountOperation.SellCREDD;
                }

                IPlayer buyer = playerManager.GetPlayerByAccountId(buyOrder.AccountId);
                GrantCreddEntitlement(buyOrder.AccountId, buyer?.Account);
                session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, matchedCreditAmount);
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

            CREDDOrder restingSellOrder;
            lock (syncRoot)
            {
                restingSellOrder = new CREDDOrder
                {
                    OrderId      = nextOrderId++,
                    AccountId    = session.Account.Id,
                    CharacterId  = session.Player.CharacterId,
                    Identity     = CloneIdentity(session.Player.Identity, session.Player.CharacterId),
                    CreditAmount = creditAmount,
                    IsBuyOrder   = false
                };
            }

            if (!TryCommitAddOrder(restingSellOrder))
            {
                lock (syncRoot)
                {
                    if (nextOrderId == restingSellOrder.OrderId + 1ul)
                        nextOrderId--;
                }

                result = AccountOperationResult.GenericFail;
                return AccountOperation.SellCREDD;
            }

            RecordHistory(session.Account.Id, session.Player, AccountOperation.SellCREDD, creditAmount, true);

            result = AccountOperationResult.Ok;
            return AccountOperation.SellCREDD;
        }

        public AccountOperationResult CancelOrder(IWorldSession session, ulong orderId)
        {
            if (session.Player == null)
                return AccountOperationResult.GenericFail;

            EnsureLoaded();

            CREDDOrder order;
            lock (syncRoot)
            {
                order = orders.FirstOrDefault(o => o.OrderId == orderId && o.AccountId == session.Account.Id);
            }

            if (order == null)
                return AccountOperationResult.NoMatchingOrder;

            if (!TryReserveRemoveOrder(order))
                return AccountOperationResult.GenericFail;

            if (order.IsBuyOrder)
                session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, order.CreditAmount);
            RecordHistory(session.Account.Id, session.Player, AccountOperation.CancelCREDDOrder, order.CreditAmount, true);

            return AccountOperationResult.Ok;
        }

        private void RecordHistory(uint accountId, IPlayer player, AccountOperation operation, ulong creditAmount, bool isInitiator, NetworkIdentity counterparty = null)
        {
            RecordHistory(accountId, CloneIdentity(player?.Identity, player?.CharacterId ?? 0ul), operation, creditAmount, isInitiator, counterparty);
        }

        private void RecordHistory(uint accountId, NetworkIdentity identity, AccountOperation operation, ulong creditAmount, bool isInitiator, NetworkIdentity counterparty = null)
        {
            var entry = new CREDDHistoryEntry
            {
                AccountId    = accountId,
                CreatedUtc   = DateTime.UtcNow,
                Operation    = operation,
                IsInitiator  = isInitiator,
                Identity     = CloneIdentity(identity),
                Counterparty = CloneIdentity(counterparty),
                CreditAmount = creditAmount
            };

            lock (syncRoot)
            {
                operationHistory.Add(entry);

                int overflowRows = operationHistory.Count(h => h.AccountId == accountId) - MaxHistoryRowsPerAccount;
                if (overflowRows > 0)
                {
                    foreach (CREDDHistoryEntry oldEntry in operationHistory
                        .Where(h => h.AccountId == accountId)
                        .OrderBy(h => h.CreatedUtc)
                        .Take(overflowRows)
                        .ToList())
                    {
                        operationHistory.Remove(oldEntry);
                    }
                }
            }

            PersistHistory(entry);
        }

        private AuthDatabase TryGetAuthDatabase()
        {
            try
            {
                return databaseManager.GetDatabase<AuthDatabase>();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return null;
            }
        }

        private void EnsureLoaded()
        {
            if (loadedFromDatabase)
                return;

            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase == null)
            {
                lock (syncRoot)
                    loadedFromDatabase = true;

                return;
            }

            List<CREDDOrder> loadedOrders = authDatabase.GetCREDDOrders()
                .Select(model => new CREDDOrder
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
                })
                .ToList();

            ulong loadedNextOrderId = loadedOrders.Count == 0
                ? 1ul
                : loadedOrders.Max(order => order.OrderId) + 1ul;

            lock (syncRoot)
            {
                if (loadedFromDatabase)
                    return;

                foreach (CREDDOrder order in loadedOrders)
                    orders.Add(order);

                nextOrderId        = Math.Max(nextOrderId, loadedNextOrderId);
                loadedFromDatabase = true;
            }
        }

        private bool TryCommitAddOrder(CREDDOrder order)
        {
            if (!TryPersistAddOrder(order))
                return false;

            lock (syncRoot)
                orders.Add(order);

            return true;
        }

        private bool TryReserveRemoveOrder(CREDDOrder order)
        {
            lock (syncRoot)
            {
                if (!orders.Remove(order))
                    return false;
            }

            if (!TryPersistRemoveOrder(order))
            {
                lock (syncRoot)
                    orders.Add(order);

                return false;
            }

            return true;
        }

        private bool TryPersistAddOrder(CREDDOrder order)
        {
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase == null)
                return true;

            try
            {
                authDatabase.UpsertCREDDOrder(new AccountCREDDOrderModel
                {
                    OrderId      = order.OrderId,
                    AccountId    = order.AccountId,
                    CharacterId  = order.CharacterId,
                    RealmId      = order.Identity.RealmId,
                    CreditAmount = order.CreditAmount,
                    IsBuyOrder   = order.IsBuyOrder
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryPersistRemoveOrder(CREDDOrder order)
        {
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase == null)
                return true;

            try
            {
                authDatabase.RemoveCREDDOrder(order.OrderId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private CharacterDatabase TryGetCharacterDatabase()
        {
            try
            {
                return databaseManager.GetDatabase<CharacterDatabase>();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return null;
            }
        }

        private void CreditCharacterCurrency(ulong characterId, ulong amount, IPlayer onlinePlayer)
        {
            if (amount == 0ul)
                return;

            if (onlinePlayer != null)
            {
                onlinePlayer.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, amount);
                return;
            }

            TryGetCharacterDatabase()?.CreditCharacterCurrency(characterId, (byte)CurrencyType.Credits, amount);
        }

        private void GrantCreddEntitlement(uint accountId, IAccount onlineAccount)
        {
            if (onlineAccount != null)
            {
                onlineAccount.EntitlementManager.UpdateEntitlement(EntitlementType.CREDDUsage, 1);
                return;
            }

            TryGetAuthDatabase()?.IncrementAccountEntitlement(accountId, (byte)EntitlementType.CREDDUsage, 1u);
        }

        private void PersistHistory(CREDDHistoryEntry entry)
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
                    Operation         = (uint)Operation,
                    IsInitiator       = IsInitiator,
                    LogAgeMinutes     = (uint)Math.Max(0d, Math.Floor((now - CreatedUtc).TotalMinutes)),
                    Identity0         = CloneIdentity(Identity),
                    Identity1         = CloneIdentity(Counterparty),
                    FriendCharacterId = Counterparty?.Id ?? 0ul,
                    MoneyAmount       = CreditAmount
                };
            }
        }
    }
}
