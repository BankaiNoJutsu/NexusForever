using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class UpstreamDashPacketTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(4u)]
    [InlineData(5u)]
    [InlineData(6u)]
    [InlineData(7u)]
    public void RawDirectionUsesThreeBitsAndLeavesFollowingBits(uint direction)
    {
        // Low three bits are direction; the remaining five hold sentinel 0x15.
        using var reader = new GamePacketReader(new MemoryStream([(byte)(0xA8u | direction)]));
        var message = new ClientDashCast();
        message.Read(reader);
        Assert.Equal(direction, message.DirectionOrState);
        Assert.Equal(0x15u, reader.ReadUInt(5));
    }

    [Fact]
    public void EmptyBodyRetainsExistingRecoveryWithoutReadException()
    {
        using var reader = new GamePacketReader(new MemoryStream([]));
        var message = new ClientDashCast();
        message.Read(reader);
        Assert.Equal(0u, message.DirectionOrState);
    }

    [Fact]
    public void ExtraBytesAreNotConsumedAsPartOfDirection()
    {
        using var reader = new GamePacketReader(new MemoryStream([0x05, 0xA5, 0x5A, 0xFF]));
        var message = new ClientDashCast();
        message.Read(reader);
        Assert.Equal(5u, message.DirectionOrState);
        Assert.Equal(0u, reader.ReadUInt(5));
        Assert.Equal((byte)0xA5, reader.ReadByte());
        Assert.Equal((byte)0x5A, reader.ReadByte());
        Assert.Equal((byte)0xFF, reader.ReadByte());
    }
}
