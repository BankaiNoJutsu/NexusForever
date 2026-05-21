using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Guild;

public class WarPartyBossTokenProtocolTests
{
    [Fact]
    public void ClientWarPartyBossTokensRequest_ReadsGuildIdentity()
    {
        byte[] packetData = WritePacket(writer =>
        {
            new Identity
            {
                RealmId = 7,
                Id = 0x123456789ul
            }.Write(writer);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientWarPartyBossTokensRequest();

        packet.Read(reader);

        Assert.Equal((ushort)7, packet.GuildIdentity.RealmId);
        Assert.Equal(0x123456789ul, packet.GuildIdentity.Id);
    }

    [Fact]
    public void ServerWarPartyBossTokens_WritesGuildIdentityAndTokenRows()
    {
        var packet = new ServerWarPartyBossTokens
        {
            GuildIdentity = new Identity
            {
                RealmId = 3,
                Id = 0x99887766ul
            },
            Tokens =
            {
                new WarPartyBossToken
                {
                    TokenItem2Id = 0x12345u,
                    Count = 4u
                }
            }
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal((ushort)3, reader.ReadUShort(14u));
        Assert.Equal(0x99887766ul, reader.ReadULong());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal(4u, reader.ReadUInt());
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
