using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class TradePacketShapeTests
{
    [Fact]
    public void ServerP2PTradeUpdateItem_WritesMappedItemIdentityAndStackCount()
    {
        var packet = new ServerP2PTradeUpdateItem
        {
            TradeIndex = 0x11223344u,
            OwnerUnitId = 0x55667788u,
            ItemId = 0x2AAAAu,
            ItemGuid = 0x0102030405060708ul,
            StackCount = 0x99AABBCCu,
            RandomCircuitData = 0x1112131415161718ul,
            RandomGlyphData = 0x21222324u,
            ThresholdData = 0x3132333435363738ul,
            WorldRequirement_Item2Id = 0x2BBBbu,
            Item2IdMicrochip = [1u, 2u, 3u, 4u, 5u]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal(0x2AAAAu, reader.ReadUInt(18u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x99AABBCCu, reader.ReadUInt());
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x3132333435363738ul, reader.ReadULong());
        Assert.Equal(0x2BBBbu, reader.ReadUInt(18u));

        for (uint expected = 1u; expected <= 5u; expected++)
        {
            Assert.Equal(expected, reader.ReadUInt());
        }
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
