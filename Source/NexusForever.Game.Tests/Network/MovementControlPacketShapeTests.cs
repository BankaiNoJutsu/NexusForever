using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class MovementControlPacketShapeTests
{
    [Fact]
    public void ServerMovementControl_WritesTicketImmediateAndUnitId()
    {
        var packet = new ServerMovementControl
        {
            Ticket = 1u,
            Immediate = true,
            UnitId = 0x11223344u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(0x11223344u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMovementControlRemove_WritesEmptyPayload()
    {
        Assert.Empty(WritePacket(new ServerMovementControlRemove()));
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}