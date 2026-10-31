using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class UpstreamVehiclePacketTests
{
    [Fact]
    public void VehicleEmbarkReadsPackedContextWithoutConsumingFollowingByte()
    {
        // Native writer140089250: unit32, context2, context3, then sentinel8.
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString("EFCDAB89B714")));
        var message = new ClientVehicleEmbark();
        message.Read(reader);
        Assert.Equal(0x89ABCDEFu, message.VehicleUnitId);
        Assert.Equal(3u, message.Unknown1);
        Assert.Equal(5u, message.Unknown2);
        Assert.Equal((byte)0xA5, reader.ReadByte());
        Assert.Equal(0, reader.ReadByte(3u));
        Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
    }

    [Fact]
    public void VehicleEmbarkAcceptsFiveByteNativeRequest()
    {
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString("EFCDAB891F")));
        var message = new ClientVehicleEmbark();
        message.Read(reader);
        Assert.Equal(0x89ABCDEFu, message.VehicleUnitId);
        Assert.Equal(3u, message.Unknown1);
        Assert.Equal(7u, message.Unknown2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    public void VehicleEmbarkRejectsTruncatedRequest(int length)
    {
        byte[] complete = Convert.FromHexString("EFCDAB891F");
        using var reader = new GamePacketReader(new MemoryStream(complete[..length]));
        Assert.Throws<EndOfStreamException>(() => new ClientVehicleEmbark().Read(reader));
    }
}
