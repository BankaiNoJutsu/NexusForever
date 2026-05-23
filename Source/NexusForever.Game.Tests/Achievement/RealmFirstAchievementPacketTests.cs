using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.Game.Tests.Achievement;

public class RealmFirstAchievementPacketTests
{
    [Fact]
    public void ServerAchievementInit_WritesCountedAchievementRows()
    {
        var packet = new ServerAchievementInit
        {
            Achievements =
            [
                new NexusForever.Network.World.Message.Model.Achievement.Achievement
                {
                    AchievementId = 0x1234,
                    ProgressState         = 0x01020304u,
                    CreditedChecklistMask = 0x05060708u,
                    DateCompleted = 0x1112131415161718ul
                }
            ]
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(1u, reader.ReadUInt());
        AssertAchievement(reader, 0x1234, 0x01020304u, 0x05060708u, 0x1112131415161718ul);
    }

    [Fact]
    public void ServerAchievementUpdate_WritesDeletedFlagAndRows()
    {
        var packet = new ServerAchievementUpdate
        {
            Deleted = true,
            Achievements =
            [
                new NexusForever.Network.World.Message.Model.Achievement.Achievement
                {
                    AchievementId = 0x2345,
                    ProgressState         = 0x21222324u,
                    CreditedChecklistMask = 0x25262728u,
                    DateCompleted = 0x3132333435363738ul
                }
            ]
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.True(reader.ReadBit());
        Assert.Equal(1u, reader.ReadUInt());
        AssertAchievement(reader, 0x2345, 0x21222324u, 0x25262728u, 0x3132333435363738ul);
    }

    [Fact]
    public void ServerRealmFirstAchievement_WritesAchievementGuildFlagAndName()
    {
        var packet = new ServerRealmFirstAchievement
        {
            AchievementId      = 0x1234,
            IsGuildAchievement = true,
            Name               = "First Guild"
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal((ushort)0x1234, reader.ReadUShort(15u));
        Assert.True(reader.ReadBit());
        Assert.Equal("First Guild", reader.ReadWideString());
    }

    private static void AssertAchievement(GamePacketReader reader, ushort achievementId, uint progressState, uint creditedChecklistMask, ulong dateCompleted)
    {
        Assert.Equal(achievementId, reader.ReadUShort(15u));
        Assert.Equal(progressState, reader.ReadUInt());
        Assert.Equal(creditedChecklistMask, reader.ReadUInt());
        Assert.Equal(dateCompleted, reader.ReadULong());
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
