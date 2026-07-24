using System;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    internal static class VendorPriceCalculator
    {
        public static float GetPurchaseMultiplier(IPlayer player)
        {
            return Combine(
                player?.SelectedVendorInfo?.SellPriceMultiplier ?? 1f,
                player?.VendorSellPriceMultiplier ?? 1f);
        }

        public static float GetSalePayoutMultiplier(IPlayer player)
        {
            return Combine(
                player?.SelectedVendorInfo?.BuyPriceMultiplier ?? 1f,
                player?.VendorBuyPriceMultiplier ?? 1f);
        }

        public static ulong ApplyPurchaseMultiplier(ulong amount, float multiplier)
        {
            return ApplyMultiplier(amount, multiplier, decimal.Ceiling);
        }

        public static ulong ApplySalePayoutMultiplier(ulong amount, float multiplier)
        {
            return ApplyMultiplier(amount, multiplier, decimal.Floor);
        }

        public static ulong CalculatePurchaseCost(uint unitAmount, uint purchaseQuantity)
        {
            return (ulong)unitAmount * purchaseQuantity;
        }

        public static bool TryCalculateGrantedItemCount(uint purchaseQuantity, uint stackCount, out uint itemCount)
        {
            ulong count = (ulong)purchaseQuantity * stackCount;
            if (count == 0ul || count > uint.MaxValue)
            {
                itemCount = 0u;
                return false;
            }

            itemCount = (uint)count;
            return true;
        }

        private static ulong ApplyMultiplier(ulong amount, float multiplier, Func<decimal, decimal> round)
        {
            try
            {
                decimal adjusted = round(amount * (decimal)Normalise(multiplier));
                return adjusted >= ulong.MaxValue ? ulong.MaxValue : (ulong)adjusted;
            }
            catch (OverflowException)
            {
                return ulong.MaxValue;
            }
        }

        private static float Combine(float vendorMultiplier, float playerMultiplier)
        {
            float combined = Normalise(vendorMultiplier) * Normalise(playerMultiplier);
            return Normalise(combined);
        }

        private static float Normalise(float multiplier)
        {
            return float.IsFinite(multiplier) && multiplier > 0f ? multiplier : 1f;
        }
    }
}
