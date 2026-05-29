using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class PlayerPacketShapeTests
{
    [Fact]
    public void ServerPlayerCreateFactionReputation_WritesReputationAmount()
    {
        var packet = new ServerPlayerCreate.Faction.FactionReputation
        {
            FactionId = (Faction)0x1234,
            ReputationAmount = 12.5f
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal((Faction)0x1234, reader.ReadEnum<Faction>(14u));
        Assert.Equal(12.5f, reader.ReadSingle());
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
