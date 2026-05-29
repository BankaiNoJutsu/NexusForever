using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Reputation;

namespace NexusForever.Game.Tests.Network;

public class ReputationPacketShapeTests
{
    [Fact]
    public void ServerReputationUpdate_WritesFactionAndSignedDelta()
    {
        var packet = new ServerReputationUpdate
        {
            FactionId       = Faction.Dominion,
            ReputationDelta = -123.5f
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(Faction.Dominion, reader.ReadEnum<Faction>(14u));
        Assert.Equal(-123.5f, reader.ReadSingle());
    }

    [Fact]
    public void ServerReputationOverrideAdd_WritesTargetFactionAndOverrideAmount()
    {
        var packet = new ServerReputationOverrideAdd
        {
            UnitId = 0x11223344u,
            FactionId = Faction.Exile,
            ReputationAmount = 9876.5f
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(Faction.Exile, reader.ReadEnum<Faction>(14u));
        Assert.Equal(9876.5f, reader.ReadSingle());
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}
