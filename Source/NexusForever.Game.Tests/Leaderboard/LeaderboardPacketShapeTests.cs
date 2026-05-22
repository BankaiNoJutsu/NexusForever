using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.Game.Tests.Leaderboard;

public class LeaderboardPacketShapeTests
{
    [Fact]
    public void ServerLeaderboardPve_WritesScopedRowsAndTeamMembers()
    {
        var packet = new ServerLeaderboardPve
        {
            Type              = LeaderboardType.PveDungeon,
            MatchingGameMapId = 1234u,
            PrimeLevel        = 5u,
            NextUpdateTime    = 0x0102030405060708ul,
            Players =
            [
                new LeaderboardPlayerPve
                {
                    GuildId           = 0x1112131415161718ul,
                    Class             = Class.Esper,
                    MatchingGameMapId = 1234u,
                    PrimeLevel        = 5u,
                    RewardedTier      = 3u,
                    CompletionTime    = 412000u,
                    Rank              = 2u,
                    LastRank          = 4u,
                    Name              = "Archive Gold",
                    TeamMembers =
                    [
                        new TeamMember { Name = "Tank", Class = Class.Warrior },
                        new TeamMember { Name = "Healer", Class = Class.Medic }
                    ]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(LeaderboardType.PveDungeon, reader.ReadEnum<LeaderboardType>(4u));
        Assert.Equal(1234u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(1u, reader.ReadUInt());

        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(Class.Esper, reader.ReadEnum<Class>(5u));
        Assert.Equal(1234u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(412000u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal("Archive Gold", reader.ReadWideString());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("Tank", reader.ReadWideString());
        Assert.Equal(Class.Warrior, reader.ReadEnum<Class>(5u));
        Assert.Equal("Healer", reader.ReadWideString());
        Assert.Equal(Class.Medic, reader.ReadEnum<Class>(5u));
    }

    [Fact]
    public void ServerLeaderboardPvp_WritesTeamRowsAndMembers()
    {
        var packet = new ServerLeaderboardPvp
        {
            Type           = LeaderboardType.Arena3v3,
            NextUpdateTime = 0x2122232425262728ul,
            Players =
            [
                new LeaderboardTeamPvp
                {
                    GuildId     = 0x3132333435363738ul,
                    Class       = Class.PvpTeam,
                    Rating      = 2100u,
                    Rank        = 7u,
                    LastRank    = 9u,
                    Name        = "Rated Team",
                    TeamMembers =
                    [
                        new TeamMember { Name = "Lead", Class = Class.Spellslinger }
                    ]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(LeaderboardType.Arena3v3, reader.ReadEnum<LeaderboardType>(4u));
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(1u, reader.ReadUInt());

        Assert.Equal(0x3132333435363738ul, reader.ReadULong());
        Assert.Equal(Class.PvpTeam, reader.ReadEnum<Class>(5u));
        Assert.Equal(2100u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(9u, reader.ReadUInt());
        Assert.Equal("Rated Team", reader.ReadWideString());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal("Lead", reader.ReadWideString());
        Assert.Equal(Class.Spellslinger, reader.ReadEnum<Class>(5u));
    }

    private static byte[] WritePacket(IWritable message)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
