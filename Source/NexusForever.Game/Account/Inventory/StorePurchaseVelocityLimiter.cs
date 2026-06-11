using System.Collections.Concurrent;
using NexusForever.Database.Auth;

namespace NexusForever.Game.Account.Inventory
{
    /// <summary>
    /// Conservative storefront purchase velocity gate backed by auth DB history when available.
    /// </summary>
    public static class StorePurchaseVelocityLimiter
    {
        public const int MaxPurchasesPerWindow = 10;
        public static readonly TimeSpan Window = TimeSpan.FromHours(1);

        private static readonly ConcurrentDictionary<uint, List<DateTime>> inMemoryPurchases = new();

        public static bool IsWithinVelocityLimit(uint accountId)
        {
            return IsWithinVelocityLimit(accountId, null);
        }

        public static bool IsWithinVelocityLimit(uint accountId, AuthDatabase authDatabase)
        {
            DateTime cutoff = DateTime.UtcNow - Window;
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
            TryNotePurchase(accountId);
        }

        public static bool TryNotePurchase(uint accountId)
        {
            return TryNotePurchase(accountId, null);
        }

        public static bool TryNotePurchase(uint accountId, AuthDatabase authDatabase)
        {
            if (authDatabase != null)
                return true;

            DateTime cutoff = DateTime.UtcNow - Window;
            List<DateTime> purchases = inMemoryPurchases.GetOrAdd(accountId, _ => []);
            lock (purchases)
            {
                purchases.RemoveAll(time => time < cutoff);
                if (purchases.Count >= MaxPurchasesPerWindow)
                    return false;

                purchases.Add(DateTime.UtcNow);
                return true;
            }
        }
    }
}
