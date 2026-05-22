using NexusForever.Game.Static.Group;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class GroupPacketShapeTests
{
    public static TheoryData<IWritable, int> ServerGroupUnresolvedPacketCases()
    {
        return new TheoryData<IWritable, int>
        {
            { new ServerGroupInstanceDifficultyResponse(), 0x18 },
            { new ServerGroupKickResult(), 0x0C },
            { new ServerGroupLootRuleValidationResult(), 0x18 },
            { new ServerGroupRosterUpdate(), 0x60 },
            { new ServerGroupMemberRoleChange(), 0x28 },
            { new ServerGroupReadyCheckStatusUpdate(), 0x38 },
            { new ServerGroupRequestJoinWindow(), 0x20 },
            { new ServerQuestShareResult(), 0x10 },
            { new ServerGroupMemberDetailUpdate(), 0x28 },
            { new ServerRaidQueueStatus(), 0x20 }
        };
    }

    [Theory]
    [MemberData(nameof(ServerGroupUnresolvedPacketCases))]
    public void ServerGroupUnresolvedPackets_WriteKnownRawPayloadLength(IWritable packet, int expectedLength)
    {
        byte[] packetData = WritePacket(packet);

        Assert.Equal(expectedLength, packetData.Length);
    }

    [Fact]
    public void ClientGroupLootRulesChange_ReadsFourMappedRuleFields()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x1122334455667788ul);
            writer.Write(LootRule.RoundRobin, 3u);
            writer.Write(LootRule.Master, 3u);
            writer.Write(LootThreshold.Superb, 4u);
            writer.Write(HarvestLootRule.FirstTagger, 2u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientGroupLootRulesChange();

        packet.Read(reader);

        Assert.Equal(0x1122334455667788ul, packet.GroupId);
        Assert.Equal(LootRule.RoundRobin, packet.LootRulesUnderThreshold);
        Assert.Equal(LootRule.Master, packet.LootRulesThresholdAndOver);
        Assert.Equal(LootThreshold.Superb, packet.Threshold);
        Assert.Equal(HarvestLootRule.FirstTagger, packet.HarvestingRule);
    }

    [Fact]
    public void ServerGroupLootRulesChange_WritesFourMappedRuleFieldsAfterReservedDword()
    {
        var packet = new ServerGroupLootRulesChange
        {
            GroupId                   = 0x8877665544332211ul,
            UnknownDWord              = 0xAABBCCDDu,
            LootRulesUnderThreshold   = LootRule.FreeForAll,
            LootRulesThresholdAndOver = LootRule.NeedBeforeGreed,
            LootThreshold             = LootThreshold.Legendary,
            HarvestLootRule           = HarvestLootRule.RoundRobin
        };

        byte[] packetData = WritePacket(packet.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x8877665544332211ul, reader.ReadULong());
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal(LootRule.FreeForAll, reader.ReadEnum<LootRule>(3u));
        Assert.Equal(LootRule.NeedBeforeGreed, reader.ReadEnum<LootRule>(3u));
        Assert.Equal(LootThreshold.Legendary, reader.ReadEnum<LootThreshold>(4u));
        Assert.Equal(HarvestLootRule.RoundRobin, reader.ReadEnum<HarvestLootRule>(2u));
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    private static byte[] WritePacket(IWritable message)
    {
        return WritePacket(message.Write);
    }
}
