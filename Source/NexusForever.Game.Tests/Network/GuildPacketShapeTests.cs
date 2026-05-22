using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Tests.Network;

public class GuildPacketShapeTests
{
    [Fact]
    public void ClientRecruitmentGuildGetDetailedGuildInfo_ReadsGuildIdentity()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write((ushort)9, 14u);
            writer.Write(0x1122334455667788ul);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRecruitmentGuildGetDetailedGuildInfo();

        packet.Read(reader);

        Assert.Equal((ushort)9, packet.GuildIdentity.RealmId);
        Assert.Equal(0x1122334455667788ul, packet.GuildIdentity.Id);
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
