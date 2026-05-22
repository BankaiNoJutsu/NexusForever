using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Tests.Network;

public class ActionSetUnresolvedPacketShapeTests
{
    [Fact]
    public void ServerLasOpeningUInt18_WritesMapped18BitSpell4Id()
    {
        byte[] packetData = WritePacket(new ServerLasOpeningUInt18 { Spell4Id = 0x2ABCDu });

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0x2ABCDu, reader.ReadUInt(18u));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerActionSetDualUInt32Lists_WritesPairedCountAndValues()
    {
        var packet = new ServerActionSetDualUInt32Lists
        {
            Value0 = 2u,
            Value1 = 0x22222222u
        };
        packet.ValuesA.AddRange([10u, 20u, 30u]);
        packet.ValuesB.AddRange([40u, 50u, 60u]);

        byte[] packetData = WritePacket(packet);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(2u, reader.ReadUInt(4u));
        Assert.Equal(0x22222222u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt(4u));
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
        Assert.Equal(40u, reader.ReadUInt());
        Assert.Equal(50u, reader.ReadUInt());
        Assert.Equal(60u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerActionSetUInt16_WritesMapped16BitScalar()
    {
        byte[] packetData = WritePacket(new ServerActionSetUInt16 { Value = 0xBEEF });

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0xBEEFu, reader.ReadUInt(16u));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerActionSetUInt32_WritesMapped32BitScalar()
    {
        byte[] packetData = WritePacket(new ServerActionSetUInt32 { Value = 0xCAFEBABEu });

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerActionSetUInt16List_Writes7BitCountAndUshortRows()
    {
        var packet = new ServerActionSetUInt16List();
        packet.Values.AddRange([(ushort)100, (ushort)200, (ushort)300]);

        byte[] packetData = WritePacket(packet);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(3u, reader.ReadUInt(7u));
        Assert.Equal((ushort)100, reader.ReadUShort());
        Assert.Equal((ushort)200, reader.ReadUShort());
        Assert.Equal((ushort)300, reader.ReadUShort());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerActionSetFiveUInt32_WritesMappedFields()
    {
        var packet = new ServerActionSetFiveUInt32
        {
            Value0 = 1u,
            Value1 = 2u,
            Value2 = 3u,
            Value3 = 4u,
            Value4 = 5u
        };

        byte[] packetData = WritePacket(packet);

        Assert.Equal(20, packetData.Length);
        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
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
