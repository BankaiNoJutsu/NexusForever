using System.IO;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class EntityVisualPacketShapeTests
{
    [Fact]
    public void ServerEntityVisualInfoUpdate_WritesCompactCreatureDisplayPayload()
    {
        byte[] data = WritePacket(new ServerEntityVisualInfoUpdate
        {
            UnitId       = 0x10203040u,
            CreatureId   = 0x12345u,
            DisplayInfo  = 0x1234u,
            UnknownFlag0 = true,
            UnknownFlag1 = false
        });

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal(0x1234u, reader.ReadUInt(17u));
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
    }

    private static byte[] WritePacket(NexusForever.Network.Message.IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
