using System.Collections.Frozen;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Inventory
{
    public static class CouponRedemptionService
    {
        private static readonly FrozenDictionary<string, uint> KnownCoupons = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            ["NEXUSFOREVER"] = 77u,
            ["WELCOME"]      = 77u,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        public static AccountOperationResult TryRedeem(IAccount account, string couponCode, out uint accountItemId)
        {
            accountItemId = 0u;
            if (account == null)
                return AccountOperationResult.GenericFail;

            if (string.IsNullOrWhiteSpace(couponCode))
                return AccountOperationResult.InvalidCoupon;

            if (!KnownCoupons.TryGetValue(couponCode.Trim(), out accountItemId))
                return AccountOperationResult.InvalidCoupon;

            if (account.InventoryManager == null)
                return AccountOperationResult.GenericFail;

            if (!account.InventoryManager.CanAddItem(accountItemId))
                return AccountOperationResult.InvalidAccountItem;

            account.InventoryManager.AddItem(accountItemId);
            return AccountOperationResult.Ok;
        }
    }
}
