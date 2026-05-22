using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.Game.Tests.Achievement;

public class RealmFirstAchievementPacketTests
{
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

    private static byte[] WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
