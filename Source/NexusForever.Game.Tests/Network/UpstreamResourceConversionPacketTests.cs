using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Item;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Network;

public class UpstreamResourceConversionPacketTests
{
    // Raw little-endian bit fixtures, independently assembled from the native
    // writer's 14+32+64 layout and an eight-bit following sentinel.
    [Theory]
    [InlineData(0u, "00C07BF36A22840C951DA62EB77F29")]
    [InlineData(0x2ABCu, "BCEA7BF36A22840C951DA62EB77F29")]
    [InlineData(0x3FFFu, "FFFF7BF36A22840C951DA62EB77F29")]
    public void ConvertResourceReadsPackedIdAndPreservesHighFieldsAndFollowingByte(uint id, string fixture)
    {
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString(fixture)));
        var message = new ClientConvertResource();
        message.Read(reader);
        Assert.Equal(id, message.ConversionId);
        Assert.Equal(0x89ABCDEFu, message.Selector);
        Assert.Equal(0xFEDCBA9876543210ul, message.ResourceField);
        Assert.Equal((byte)0xA5, reader.ReadByte());
        Assert.Equal(0, reader.ReadByte(2u));
        Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
    }

    [Fact]
    public void ConvertResourceAcceptsMinimalFourteenByteNativePayload()
    {
        // Last two bits are packet padding rather than part of the resource id.
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString("BCEA7BF36A22840C951DA62EB73F")));
        var message = new ClientConvertResource();
        message.Read(reader);
        Assert.Equal(0x2ABCu, message.ConversionId);
        Assert.Equal(0x89ABCDEFu, message.Selector);
        Assert.Equal(0xFEDCBA9876543210ul, message.ResourceField);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(13)]
    public void ConvertResourceRejectsTruncationAtEachField(int length)
    {
        byte[] complete = Convert.FromHexString("BCEA7BF36A22840C951DA62EB73F");
        using var reader = new GamePacketReader(new MemoryStream(complete[..length]));
        Assert.Throws<EndOfStreamException>(() => new ClientConvertResource().Read(reader));
    }

    [Fact]
    public void TradePartnerListWritesNativeIdentityAndTimerLayout()
    {
        var message = new ServerItemTradePartners { ItemGuid = 0x0102030405060708ul, TradeTimeLeftMs = 0x12345678u };
        message.TradingPartnerIdentities.Add(new Identity { RealmId = 0x1234, Id = 0xFEDCBA9876543210ul });
        byte[] actual = Encode(writer => { message.Write(writer); writer.Write((byte)0xA5); });
        Assert.Equal(Convert.FromHexString("0807060504030201010000003412840C951DA62EB73F9E158D4429"), actual);
    }

    [Fact]
    public void TradePartnerListRetainsZeroTimerDefaultWithoutInventingState()
    {
        var message = new ServerItemTradePartners { ItemGuid = 0x0102030405060708ul };
        Assert.Equal(Convert.FromHexString("08070605040302010000000000000000"), Encode(message.Write));
    }

    private static byte[] Encode(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
