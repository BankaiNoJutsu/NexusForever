using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Map;

namespace NexusForever.Game.Tests.Map;

public class ZoneMapPacketShapeTests
{
    [Fact]
    public void ServerZoneMap_WritesMapIdByteCountAndBitBuffer()
    {
        var bits = new NetworkBitArray(16u, NetworkBitArray.BitOrder.LeastSignificantBit);
        bits.SetBit(0u, true);
        bits.SetBit(9u, true);

        var packet = new ServerZoneMap
        {
            ZoneMapId   = 123u,
            Count       = 16u,
            ZoneMapBits = bits
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(123u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal([0x01, 0x02], reader.ReadBytes(2u));
    }

    [Fact]
    public void ServerMapUpdateHexGroup_WritesMappedHexGroupFields()
    {
        var packet = new ServerMapUpdateHexGroup
        {
            MapZoneHexGroupId      = 0x1234u,
            TooltipLocalizedTextId = 0x12345u,
            Color                  = 0xAABBCCDDu,
            IsVisible              = true
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x12345u, reader.ReadUInt(21u));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.True(reader.ReadBit());
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
