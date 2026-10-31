using System.Buffers.Binary;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public class VendorPriceWireOwnershipTests
{
    [Theory]
    [InlineData(0.75f, 1.25f, 75ul, 125ul)]
    [InlineData(0.5f, 2f, 50ul, 200ul)]
    [InlineData(1.5f, 0.625f, 150ul, 62ul)]
    public void VendorPerspectiveChannels_MatchNativePurchaseAndSaleWireOwnership(
        float vendorSell, float vendorBuy, ulong purchaseCost, ulong salePayout)
    {
        var vendor = new VendorInfo(new EntityModel
        {
            EntityVendor = new EntityVendorModel { Id = 123, SellPriceMultiplier = vendorSell, BuyPriceMultiplier = vendorBuy }
        });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.SelectedVendorInfo), vendor);
        proxy.SetProperty(nameof(IPlayer.VendorSellPriceMultiplier), 1f);
        proxy.SetProperty(nameof(IPlayer.VendorBuyPriceMultiplier), 1f);

        Assert.Equal(purchaseCost, VendorPriceCalculator.ApplyPurchaseMultiplier(100, VendorPriceCalculator.GetPurchaseMultiplier(player)));
        Assert.Equal(salePayout, VendorPriceCalculator.ApplySalePayoutMultiplier(100, VendorPriceCalculator.GetSalePayoutMultiplier(player)));

        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            vendor.Build().Write(writer);
            writer.FlushBits();
            // Empty category/item arrays leave the native two floats at byte12
            // and byte16. Native 140097ca0 -> 1403db9a0 -> Item.GetBuyPrice
            // (14041bf80) consumes the first; GetSellPrice(14041bcc0) the second.
            byte[] bytes = stream.ToArray();
            Assert.Equal(vendorSell, BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12))));
            Assert.Equal(vendorBuy, BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16))));
        }
    }
}
