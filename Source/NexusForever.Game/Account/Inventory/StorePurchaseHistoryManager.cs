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

        public static void RecordPurchase(uint accountId, uint offerId, ushort currencyId, ulong price)
        {
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase != null)
            {
                authDatabase.AddStorePurchaseHistory(new AccountStorePurchaseHistoryModel
                {
                    AccountId    = accountId,
                    OfferId      = offerId,
                    CurrencyId   = currencyId,
                    Price        = price,
                    PurchasedUtc = DateTime.UtcNow
                });
                return;
            }

            StorePurchaseVelocityLimiter.NotePurchase(accountId);
        }

        public static void SendPurchaseHistory(IAccount account)
        {
            if (account?.Session == null)
                return;

            var message = new ServerStorePurchaseHistoryReady();

            AuthDatabase authDatabase = TryGetAuthDatabase();
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
                    Value8      = 0u
                });
            }

            log.Info($"StorefrontCatalogDiagnostics account {account.Id}: sending ServerStorePurchaseHistoryReady rows={message.Rows.Count} offerIds=[{string.Join(",", rows.Select(row => row.OfferId))}].");

            account.Session.EnqueueMessageEncrypted(message);
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
    }
}
