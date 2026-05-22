using System.Collections.Concurrent;
using NexusForever.Database;
using NexusForever.Database.Auth;

namespace NexusForever.Game.Account.Inventory
{
    /// <summary>
    /// Conservative storefront purchase velocity gate backed by auth DB history when available.
    /// </summary>
    public static class StorePurchaseVelocityLimiter
    {
        private const int MaxPurchasesPerWindow = 10;
        private static readonly TimeSpan Window = TimeSpan.FromHours(1);

        private static readonly ConcurrentDictionary<uint, List<DateTime>> inMemoryPurchases = new();

        public static bool IsWithinVelocityLimit(uint accountId)
        {
            DateTime cutoff = DateTime.UtcNow - Window;
            AuthDatabase authDatabase = TryGetAuthDatabase();
            if (authDatabase != null)
                return authDatabase.CountStorePurchasesSince(accountId, cutoff) < MaxPurchasesPerWindow;

            if (!inMemoryPurchases.TryGetValue(accountId, out List<DateTime> purchases))
                return true;

            lock (purchases)
            {
                purchases.RemoveAll(time => time < cutoff);
                return purchases.Count < MaxPurchasesPerWindow;
            }
        }

        public static void NotePurchase(uint accountId)
        {
            if (TryGetAuthDatabase() != null)
                return;

            List<DateTime> purchases = inMemoryPurchases.GetOrAdd(accountId, _ => []);
            lock (purchases)
            {
                purchases.Add(DateTime.UtcNow);
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
    }
}
