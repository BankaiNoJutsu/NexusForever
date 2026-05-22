using NexusForever.Game.Static.Guild;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Shared;

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

    [Fact]
    public void ServerRecruitmentGuildDetails_WritesIdentityDescriptionAgeTaxMinimumAndDemands()
    {
        var packet = new ServerRecruitmentGuildDetails
        {
            GuildIdentity = new Identity { RealmId = 7, Id = 0x0102030405060708ul },
            Description = "Raid and tacos",
            GuildCreationDaysAgo = 12.5f,
            HasTax = true,
            RecruitmentMinimumLevel = 50u,
            Demands = CreateDemands()
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        AssertIdentity(reader, 7, 0x0102030405060708ul);
        Assert.Equal("Raid and tacos", reader.ReadWideString());
        Assert.Equal(12.5f, reader.ReadSingle());
        Assert.True(reader.ReadBit());
        Assert.Equal(50u, reader.ReadUInt());
        AssertDemands(reader);
    }

    [Fact]
    public void ServerRecruitmentGuildsList_WritesGuildRowsWithRecruiters()
    {
        var packet = new ServerRecruitmentGuildsList
        {
            Guilds =
            [
                new RecruitmentGuildInfo
                {
                    GuildId = 0x1112131415161718ul,
                    GuildName = "Packet Guild",
                    GuildMasterName = "Founder",
                    Stats = new GuildStats
                    {
                        MemberCount = 42,
                        PerkCount = 3,
                        Classification = GuildClassification.Raiding
                    },
                    Recruiters = ["Alice", "Bob"]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        AssertRecruitmentGuildInfo(reader, includeRecruiters: true);
    }

    [Fact]
    public void ServerRecruitmentGuildsUpdate_WritesGuildRowsWithoutRecruiters()
    {
        var packet = new ServerRecruitmentGuildsUpdate
        {
            Guilds =
            [
                new RecruitmentGuildInfo
                {
                    GuildId = 0x1112131415161718ul,
                    GuildName = "Packet Guild",
                    GuildMasterName = "Founder",
                    Stats = new GuildStats
                    {
                        MemberCount = 42,
                        PerkCount = 3,
                        Classification = GuildClassification.Raiding
                    },
                    Recruiters = ["Alice", "Bob"]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        AssertRecruitmentGuildInfo(reader, includeRecruiters: false);
    }

    [Fact]
    public void ServerRecruitmentGuildsRecruiters_WritesNamesThenOnlineFlags()
    {
        var packet = new ServerRecruitmentGuildsRecruiters
        {
            RecruitingGuilds =
            [
                new ServerRecruitmentGuildsRecruiters.GuildRecruiters
                {
                    GuildId = 0x2122232425262728ul,
                    IsRecruiting = true,
                    RecruiterNames = ["Scout", "Officer"],
                    IsOnline = [true, false]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.True(reader.ReadBit());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("Scout", reader.ReadWideString());
        Assert.Equal("Officer", reader.ReadWideString());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
    }

    private static void AssertIdentity(GamePacketReader reader, ushort realmId, ulong id)
    {
        Assert.Equal(realmId, reader.ReadUShort(14u));
        Assert.Equal(id, reader.ReadULong());
    }

    private static RecruitmentDemands CreateDemands()
    {
        return new RecruitmentDemands
        {
            WarriorAssault = RecruitmendDemand.Low,
            WarriorSupport = RecruitmendDemand.Medium,
            EngineerAssault = RecruitmendDemand.High,
            EngineerSupport = RecruitmendDemand.Full,
            EsperAssault = RecruitmendDemand.Low,
            EsperSupport = RecruitmendDemand.Medium,
            MedicAssault = RecruitmendDemand.High,
            MedicSupport = RecruitmendDemand.Full,
            StalkedAssault = RecruitmendDemand.Low,
            StalkerSupport = RecruitmendDemand.Medium,
            SpellslingerAssault = RecruitmendDemand.High,
            SpellslingerSupport = RecruitmendDemand.Full
        };
    }

    private static void AssertDemands(GamePacketReader reader)
    {
        Assert.Equal(RecruitmendDemand.Low, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Medium, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.High, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Full, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Low, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Medium, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.High, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Full, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Low, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Medium, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.High, reader.ReadEnum<RecruitmendDemand>(8u));
        Assert.Equal(RecruitmendDemand.Full, reader.ReadEnum<RecruitmendDemand>(8u));
    }

    private static void AssertRecruitmentGuildInfo(GamePacketReader reader, bool includeRecruiters)
    {
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal("Packet Guild", reader.ReadWideString());
        Assert.Equal("Founder", reader.ReadWideString());
        Assert.Equal((ushort)42, reader.ReadUShort());
        Assert.Equal((ushort)3, reader.ReadUShort());
        Assert.Equal(GuildClassification.Raiding, reader.ReadEnum<GuildClassification>(3u));

        if (!includeRecruiters)
            return;

        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("Alice", reader.ReadWideString());
        Assert.Equal("Bob", reader.ReadWideString());
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
