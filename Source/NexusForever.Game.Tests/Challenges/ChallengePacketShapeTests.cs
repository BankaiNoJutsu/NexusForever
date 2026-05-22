using NexusForever.Game.Static.Challenges;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Tests.Challenges;

public class ChallengePacketShapeTests
{
    [Fact]
    public void ClientChallengeChoice_ReadsMappedChoicePayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x1234u, 14u);
            writer.Write(ChallengeChoice.DeclineShared, 5u);
            writer.Write(0x89ABCDEFu);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientChallengeChoice();

        packet.Read(reader);

        Assert.Equal(0x1234u, packet.ChallengeId);
        Assert.Equal(ChallengeChoice.DeclineShared, packet.Choice);
        Assert.Equal(0x89ABCDEFu, packet.Unused);
    }

    [Fact]
    public void ServerChallengeResult_WritesMappedResultAndData()
    {
        var packet = new ServerChallengeResult
        {
            ChallengeId = 0x2345,
            Result      = ChallengeResult.TierAchieved,
            Data        = 2
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x2345u, reader.ReadUShort(14u));
        Assert.Equal(ChallengeResult.TierAchieved, reader.ReadEnum<ChallengeResult>());
        Assert.Equal(2, reader.ReadInt());
    }

    [Fact]
    public void ServerChallengeSharePackets_WriteMappedChallengeIds()
    {
        byte[] sharedData = WritePacket(new ServerChallengeShared
        {
            ChallengeId  = 0x0555,
            SharerUnitId = 0xAABBCCDDu
        });
        byte[] timeoutData = WritePacket(new ServerChallengeShareTimeout
        {
            ChallengeId = 0x0666
        });

        using (var reader = new GamePacketReader(new MemoryStream(sharedData)))
        {
            Assert.Equal(0x0555u, reader.ReadUShort(14u));
            Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        }

        using (var reader = new GamePacketReader(new MemoryStream(timeoutData)))
        {
            Assert.Equal(0x0666u, reader.ReadUShort(14u));
        }
    }

    [Fact]
    public void ServerChallengeUpdate_WritesMappedActiveChallengeRows()
    {
        var packet = new ServerChallengeUpdate
        {
            ActiveChallenges =
            [
                new ServerChallengeUpdate.Challenge
                {
                    ChallengeId       = 0x0777u,
                    Type              = ChallengeType.ChecklistActivate,
                    TargetGroupId     = 11u,
                    QualifyCount      = 12u,
                    QualityTotal      = 13u,
                    CurrentCount      = 14u,
                    GoalCount         = 15u,
                    ObjectiveCompletion = 0x01020304u,
                    CurrentTier       = 1u,
                    LastRewardTier    = 2u,
                    CompletionCount   = 3u,
                    Unlocked          = true,
                    Activated         = false,
                    OnCooldown        = true,
                    LeftArea          = false,
                    TimeActivatedDt   = 100u,
                    TimeTotalActive   = 300u,
                    TimeCooldownDt    = 400u,
                    TimeTotalCooldown = 1800u,
                    TimeAreaFailDt    = 500u,
                    TimeTotalAreaFail = 10u,
                    TierGoalCount     = [10u, 20u, 30u]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x0777u, reader.ReadUInt(14u));
        Assert.Equal(ChallengeType.ChecklistActivate, reader.ReadEnum<ChallengeType>(4u));
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(12u, reader.ReadUInt());
        Assert.Equal(13u, reader.ReadUInt());
        Assert.Equal(14u, reader.ReadUInt());
        Assert.Equal(15u, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.Equal(100u, reader.ReadUInt());
        Assert.Equal(300u, reader.ReadUInt());
        Assert.Equal(400u, reader.ReadUInt());
        Assert.Equal(1800u, reader.ReadUInt());
        Assert.Equal(500u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
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
