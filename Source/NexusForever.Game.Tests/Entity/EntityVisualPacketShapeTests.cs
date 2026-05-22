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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ServerEntityVisualInfoUpdate_WritesFlagCombinations(bool unknownFlag0, bool unknownFlag1)
    {
        byte[] data = WritePacket(new ServerEntityVisualInfoUpdate
        {
            UnitId       = 0x50607080u,
            CreatureId   = 0x3FFFFu,
            DisplayInfo  = 0x1FFFFu,
            UnknownFlag0 = unknownFlag0,
            UnknownFlag1 = unknownFlag1
        });

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x3FFFFu, reader.ReadUInt(18u));
        Assert.Equal(0x1FFFFu, reader.ReadUInt(17u));
        Assert.Equal(unknownFlag0, reader.ReadBit());
        Assert.Equal(unknownFlag1, reader.ReadBit());
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
