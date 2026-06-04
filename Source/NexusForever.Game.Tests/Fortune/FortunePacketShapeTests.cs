using NexusForever.Game.Static.Fortune;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.Game.Tests.Fortune;

public class FortunePacketShapeTests
{
    private const uint ClickEmptyResetValue = 3u;

    [Fact]
    public void ClientFortuneFlipCard_ReadsSelectedCardIndex()
    {
        byte[] packetData = WritePacket(writer => writer.Write(2u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientFortuneFlipCard();
        packet.Read(reader);

        Assert.Equal(2u, packet.SelectedCardIndex);
    }

    [Fact]
    public void ServerFortuneCards_WriteSerializesOperationRarityRewardsAndFlippedState()
    {
        var message = new ServerFortuneCards
        {
            Operation = FortuneOperation.Update,
            Rarity = [RewardRarity.Normal, RewardRarity.Rare, RewardRarity.Epic],
            AccountItemId = [11u, 22u, 33u],
            CardFlipped = [true, false, true]
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(FortuneOperation.Update, reader.ReadEnum<FortuneOperation>(3u));
        Assert.Equal(RewardRarity.Normal, reader.ReadEnum<RewardRarity>(2u));
        Assert.Equal(RewardRarity.Rare, reader.ReadEnum<RewardRarity>(2u));
        Assert.Equal(RewardRarity.Epic, reader.ReadEnum<RewardRarity>(2u));
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
        Assert.Equal(33u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ServerFortuneCardUpdate_WriteSerializesRequiredFlagOperationAndFlippedState()
    {
        var message = new ServerFortuneCardUpdate
        {
            HasUpdate = true,
            Operation = FortuneOperation.Reset,
            CardFlipped = [false, true, false]
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.True(reader.ReadBit());
        Assert.Equal(FortuneOperation.Reset, reader.ReadEnum<FortuneOperation>(3u));
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
    }

    [Fact]
    public void ServerFortuneRewards_WriteSerializesRewardLists()
    {
        var message = new ServerFortuneRewards
        {
            Item2IdRewards = [101u, 202u],
            MoneyRewards =
            [
                new ServerFortuneRewards.MoneyReward
                {
                    SecondaryCurrencyAmount = 7,
                    MoneyAmount = 303u
                }
            ],
            RewardItemProbabilities = [0.25f],
            RewardMoneyProbabilities = [0.75f]
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(101u, reader.ReadUInt());
        Assert.Equal(202u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((byte)7, reader.ReadByte(5u));
        Assert.Equal(303u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0.25f, reader.ReadSingle());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0.75f, reader.ReadSingle());
    }

    [Fact]
    public void ServerFortuneReset_WriteSerializesThreeBitValue()
    {
        var message = new ServerFortuneReset
        {
            Unknown = ClickEmptyResetValue
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(ClickEmptyResetValue, reader.ReadUInt(3u));
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
