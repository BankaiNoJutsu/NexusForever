using NexusForever.Game.Entity;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public class VendorPriceModifierTests
{
    [Fact]
    public void ModifierCollection_StackCapOne_ReplacesSmallDiscountWithLargeDiscount()
    {
        var modifiers = new VendorPriceModifierCollection();

        Assert.True(modifiers.TryAdd(1u, 0.95f, 1.05f, 28618u, 49392u, 10u, 349u, 1u, out _));
        Assert.Equal(0.95f, modifiers.VendorSellMultiplier, precision: 3);
        Assert.Equal(1.05f, modifiers.VendorBuyMultiplier, precision: 3);

        Assert.True(modifiers.TryAdd(2u, 0.85f, 1.15f, 28619u, 49393u, 11u, 349u, 1u, out _));
        Assert.Equal(0.85f, modifiers.VendorSellMultiplier, precision: 3);
        Assert.Equal(1.15f, modifiers.VendorBuyMultiplier, precision: 3);
        Assert.False(modifiers.Remove(1u));
        Assert.True(modifiers.Remove(2u));
        Assert.Equal(1f, modifiers.VendorSellMultiplier);
        Assert.Equal(1f, modifiers.VendorBuyMultiplier);
    }

    [Fact]
    public void PriceCalculator_AppliesVendorSellChannelToPurchaseAndRoundsUp()
    {
        Assert.Equal(95ul, VendorPriceCalculator.ApplyPurchaseMultiplier(100ul, 0.95f));
        Assert.Equal(1ul, VendorPriceCalculator.ApplyPurchaseMultiplier(1ul, 0.85f));
    }

    [Fact]
    public void PriceCalculator_AppliesVendorBuyChannelToSalePayoutAndRoundsDown()
    {
        Assert.Equal(105ul, VendorPriceCalculator.ApplySalePayoutMultiplier(100ul, 1.05f));
        Assert.Equal(1ul, VendorPriceCalculator.ApplySalePayoutMultiplier(1ul, 1.15f));
    }

    [Fact]
    public void PriceCalculator_PurchaseArithmeticDoesNotWrapAtUintBoundary()
    {
        Assert.Equal(18446744065119617025ul, VendorPriceCalculator.CalculatePurchaseCost(uint.MaxValue, uint.MaxValue));

        Assert.False(VendorPriceCalculator.TryCalculateGrantedItemCount(uint.MaxValue, 2u, out uint rejectedCount));
        Assert.Equal(0u, rejectedCount);

        Assert.True(VendorPriceCalculator.TryCalculateGrantedItemCount(3u, 5u, out uint grantedCount));
        Assert.Equal(15u, grantedCount);
    }
}
