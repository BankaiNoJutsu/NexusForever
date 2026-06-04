using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.Game.Tests.Pregame;

public class RealmAuxPacketShapeTests
{
    [Fact]
    public void ServerRealmAuxUInt32TripletList_WritesCountedRows()
    {
        var packet = new ServerRealmAuxUInt32TripletList();
        packet.Rows.Add(new ServerSpellUInt32TripletListRow { Value0 = 1u, Value1 = 2u, Value2 = 3u });

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
    }

    private static GamePacketReader CreateReader(byte[] packetData)
    {
        return new GamePacketReader(new MemoryStream(packetData));
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
