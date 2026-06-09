using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Model.Spell;

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

    [Fact]
    public void ClientCastGuildBossToken_ReadsGuildIdentityItemAndContextToken()
    {
        byte[] packetData = WritePacket(writer =>
        {
            new Identity
            {
                RealmId = 11,
                Id = 0x1020304050607080ul
            }.Write(writer);
            writer.Write(0x23456u, 18u);
            writer.Write(0xAABBCCDDu);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCastGuildBossToken();

        packet.Read(reader);

        Assert.Equal((ushort)11, packet.GuildIdentity.RealmId);
        Assert.Equal(0x1020304050607080ul, packet.GuildIdentity.Id);
        Assert.Equal(0x23456u, packet.Item2Id);
        Assert.Equal(0xAABBCCDDu, packet.ContextToken);
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
