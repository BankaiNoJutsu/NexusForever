using System;
using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.Game.Account.Inventory
{
    public static class StorePurchaseHistoryManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const int MaxHistoryRows = 50;
        private const uint UnresolvedPurchaseHistorySubType = 0u;
        private static readonly DateTime ClientPurchaseHistoryEpochUtc = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static bool TryRecordPurchase(uint accountId, uint offerId, ushort currencyId, ulong price)
        {
            return TryRecordPurchase(null, accountId, offerId, currencyId, price);
        }

        public static bool TryRecordPurchase(AuthDatabase authDatabase, uint accountId, uint offerId, ushort currencyId, ulong price)
        {
            return TryRecordPurchase(authDatabase, accountId, offerId, currencyId, price, out _);
        }

        public static bool TryRecordPurchase(AuthDatabase authDatabase, uint accountId, uint offerId, ushort currencyId, ulong price, out ulong historyId)
        {
            if (authDatabase != null)
            {
                DateTime nowUtc = DateTime.UtcNow;
                AccountStorePurchaseHistoryModel model = CreateHistoryModel(accountId, offerId, currencyId, price, nowUtc);
                bool recorded = authDatabase.TryAddStorePurchaseHistory(
                    model,
                    nowUtc - StorePurchaseVelocityLimiter.Window,
                    StorePurchaseVelocityLimiter.MaxPurchasesPerWindow);
                historyId = recorded ? model.Id : 0ul;
                return recorded;
            }

            historyId = 0ul;
            return StorePurchaseVelocityLimiter.TryNotePurchase(accountId, authDatabase);
        }

        public static void TryDeletePurchaseHistory(AuthDatabase authDatabase, ulong historyId)
        {
            if (authDatabase == null || historyId == 0ul)
                return;

            try
            {
                authDatabase.DeleteStorePurchaseHistory(historyId);
            }
            catch (Exception ex)
            {
                log.Warn(ex, $"StorefrontCatalogDiagnostics failed to delete reserved purchase history row {historyId} after delivery failure.");
            }
        }

        public static bool IsWithinVelocityLimit(uint accountId, AuthDatabase authDatabase)
        {
            return StorePurchaseVelocityLimiter.IsWithinVelocityLimit(accountId, authDatabase);
        }

        private static AccountStorePurchaseHistoryModel CreateHistoryModel(uint accountId, uint offerId, ushort currencyId, ulong price, DateTime purchasedUtc)
        {
            return new AccountStorePurchaseHistoryModel
            {
                AccountId    = accountId,
                OfferId      = offerId,
                CurrencyId   = currencyId,
                Price        = price,
                PurchasedUtc = purchasedUtc
            };
        }

        public static void SendPurchaseHistory(IAccount account, AuthDatabase authDatabase = null)
        {
            if (account?.Session == null)
                return;

            var message = new ServerStorePurchaseHistoryReady();

            if (authDatabase == null)
            {
                log.Info($"StorefrontCatalogDiagnostics account {account.Id}: no auth database available; sending empty ServerStorePurchaseHistoryReady.");
                account.Session.EnqueueMessageEncrypted(message);
                return;
            }

            List<AccountStorePurchaseHistoryModel> rows = authDatabase.GetStorePurchaseHistory(account.Id, MaxHistoryRows);
            if (rows.Count == 0)
            {
                log.Info($"StorefrontCatalogDiagnostics account {account.Id}: purchaseHistoryRows=0; sending empty ServerStorePurchaseHistoryReady.");
                account.Session.EnqueueMessageEncrypted(message);
                return;
            }

            foreach (AccountStorePurchaseHistoryModel row in rows)
            {
                message.Rows.Add(new ServerStorePurchaseHistoryReady.Row
                {
                    Value0      = row.OfferId,
                    Value1      = row.Price,
                    UInt5Value  = row.CurrencyId,
                    UInt3Value  = UnresolvedPurchaseHistorySubType,
                    FloatValue  = row.Price,
                    StringValue = string.Empty,
                    Flag        = false,
                    Value7      = (ulong)row.Id,
                    Value8      = ToClientPurchaseHistoryDate(row.PurchasedUtc)
                });
            }

            log.Info($"StorefrontCatalogDiagnostics account {account.Id}: sending ServerStorePurchaseHistoryReady rows={message.Rows.Count} offerIds=[{string.Join(",", rows.Select(row => row.OfferId))}].");

            account.Session.EnqueueMessageEncrypted(message);
        }

        public static void SendPurchaseHistory(IAccount account, IDatabaseManager databaseManager)
        {
            SendPurchaseHistory(account, TryGetAuthDatabase(databaseManager));
        }

        public static uint ToClientPurchaseHistoryDate(DateTime purchasedUtc)
        {
            DateTime utcDate = ToUtc(purchasedUtc).Date;
            if (utcDate <= ClientPurchaseHistoryEpochUtc)
                return 0u;

            double days = (utcDate - ClientPurchaseHistoryEpochUtc).TotalDays;
            return days >= uint.MaxValue ? uint.MaxValue : (uint)days;
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc         => dateTime,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _                        => dateTime.ToUniversalTime()
            };
        }

        private static AuthDatabase TryGetAuthDatabase(IDatabaseManager databaseManager)
        {
            try
            {
                return databaseManager?.GetDatabase<AuthDatabase>();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return null;
            }
        }
    }
}
