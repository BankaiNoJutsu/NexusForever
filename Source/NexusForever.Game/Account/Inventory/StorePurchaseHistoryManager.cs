using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public static class StorePurchaseHistoryManager
    {
        private const int MaxHistoryRows = 50;

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
                account.Session.EnqueueMessageEncrypted(message);
                return;
            }

            foreach (AccountStorePurchaseHistoryModel row in authDatabase.GetStorePurchaseHistory(account.Id, MaxHistoryRows))
            {
                message.Rows.Add(new ServerStorePurchaseHistoryReady.Row
                {
                    Value0      = row.OfferId,
                    Value1      = row.Price,
                    UInt5Value  = 0u,
                    UInt3Value  = row.CurrencyId,
                    FloatValue  = row.Price,
                    StringValue = string.Empty,
                    Flag        = false,
                    Value7      = (ulong)row.Id,
                    Value8      = 0u
                });
            }

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
