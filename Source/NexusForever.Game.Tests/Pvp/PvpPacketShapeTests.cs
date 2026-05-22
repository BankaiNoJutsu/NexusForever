using NexusForever.Game.Static.Pvp;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;

namespace NexusForever.Game.Tests.Pvp;

public class PvpPacketShapeTests
{
    [Fact]
    public void ClientPvpToggleFlags_ReadsSingleBitValue()
    {
        byte[] packetData = WritePacket(writer => writer.Write(true));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientPvpToggleFlags();

        packet.Read(reader);

        Assert.True(packet.Value);
    }

    [Fact]
    public void ClientSetIgnoreDuelRequests_ReadsSingleBitValue()
    {
        byte[] packetData = WritePacket(writer => writer.Write(false));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientSetIgnoreDuelRequests();

        packet.Read(reader);

        Assert.False(packet.Ignore);
    }

    [Fact]
    public void ServerDuelParticipantPackets_WriteChallengerAndOpponentUnits()
    {
        byte[] challengeData = WritePacket(new ServerDuelChallenge
        {
            ChallengerUnitId = 0x11111111u,
            OpponentUnitId   = 0x22222222u
        });
        byte[] countdownData = WritePacket(new ServerDuelCountdown
        {
            ChallengerUnitId = 0x33333333u,
            OpponentUnitId   = 0x44444444u
        });
        byte[] startData = WritePacket(new ServerDuelStart
        {
            ChallengerUnitId = 0x55555555u,
            OpponentUnitId   = 0x66666666u
        });

        AssertUnitPair(challengeData, 0x11111111u, 0x22222222u);
        AssertUnitPair(countdownData, 0x33333333u, 0x44444444u);
        AssertUnitPair(startData, 0x55555555u, 0x66666666u);
    }

    [Fact]
    public void ServerDuelFailureAndResult_WriteMappedReasonBits()
    {
        byte[] failureData = WritePacket(new ServerDuelFailure
        {
            Reason = DuelFailureReason.PlayerIsIgnoringDuels
        });
        byte[] resultData = WritePacket(new ServerDuelResult
        {
            WinnerUnitId = 0x77777777u,
            LoserUnitId  = 0x88888888u,
            Reason       = DuelFinishReason.DuelCancelled
        });

        using (var reader = new GamePacketReader(new MemoryStream(failureData)))
        {
            Assert.Equal(DuelFailureReason.PlayerIsIgnoringDuels, reader.ReadEnum<DuelFailureReason>(5u));
        }

        using (var reader = new GamePacketReader(new MemoryStream(resultData)))
        {
            Assert.Equal(0x77777777u, reader.ReadUInt());
            Assert.Equal(0x88888888u, reader.ReadUInt());
            Assert.Equal(DuelFinishReason.DuelCancelled, reader.ReadEnum<DuelFinishReason>(3u));
        }
    }

    [Fact]
    public void ServerPvpCooldownAndStatePackets_WriteMappedFields()
    {
        byte[] cooldownData = WritePacket(new ServerPvpCooldownUpdate
        {
            CooldownRemaining = 45000u
        });
        byte[] stateData = WritePacket(new ServerUnitPvpStateChange
        {
            UnitId = 0x99999999u,
            State = PvpState.PvpOn | PvpState.Forced
        });

        using (var reader = new GamePacketReader(new MemoryStream(cooldownData)))
        {
            Assert.Equal(45000u, reader.ReadUInt());
        }

        using (var reader = new GamePacketReader(new MemoryStream(stateData)))
        {
            Assert.Equal(0x99999999u, reader.ReadUInt());
            Assert.Equal(PvpState.PvpOn | PvpState.Forced, reader.ReadEnum<PvpState>(3u));
        }
    }

    [Fact]
    public void ZeroByteDuelPackets_WriteNoPayload()
    {
        Assert.Empty(WritePacket(new ServerDuelCancelWarning()));
        Assert.Empty(WritePacket(new ServerDuelLeftArea()));
        Assert.Empty(WritePacket(new ServerPvpCooldownClear()));
    }

    private static void AssertUnitPair(byte[] packetData, uint first, uint second)
    {
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(first, reader.ReadUInt());
        Assert.Equal(second, reader.ReadUInt());
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
