using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;

namespace NexusForever.Game.Tests.Network;

public class MarketplacePacketShapeTests
{
    [Fact]
    public void AuctionInfo_ReadWriteRoundTripsMicrochipIds()
    {
        var auction = new AuctionInfo
        {
            AuctionId = 1ul,
            OwnerCharacterId = 2ul,
            MinimumBid = 3ul,
            BuyoutPrice = 4ul,
            CurrentBid = 5ul,
            TopBidderCharacterId = 6ul,
            ExpirationTime = 7ul,
            Item2Id = 0x2AAAAu,
            Quantity = 8u,
            WorldRequirement_Item2Id = 0x2BBBBu,
            MicrochipIds = [11u, 22u, 33u],
            CircuitData = 9ul,
            GlyphData = 10u,
            ThresholdData = 11ul,
            Unknown2 = 12u
        };

        byte[] packetData = WritePacket(auction);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var roundTrip = new AuctionInfo();
        roundTrip.Read(reader);

        Assert.Equal(auction.MicrochipIds, roundTrip.MicrochipIds);
        Assert.Equal(3u, (uint)roundTrip.MicrochipIds.Count);
    }

    private static byte[] WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
