using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Setting;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Network;

public class GroupPacketShapeTests
{
    public static TheoryData<IWritable, int> ServerGroupMappedPacketCases()
    {
        return new TheoryData<IWritable, int>
        {
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
    [MemberData(nameof(ServerGroupMappedPacketCases))]
    public void ServerGroupMappedPackets_WriteExpectedPayloadLength(IWritable packet, int expectedLength)
    {
        byte[] packetData = WritePacket(packet);

        Assert.Equal(expectedLength, packetData.Length);
    }

    [Fact]
    public void ServerGroupKickResult_WritesMappedFields()
    {
        var packet = new ServerGroupKickResult
        {
            GroupId = 0x1122334455667788ul,
            Result  = GroupActionResult.KickSuccess
        };

        byte[] packetData = WritePacket(packet.Write);

        Assert.Equal(0x0C, packetData.Length);
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal((uint)GroupActionResult.KickSuccess, reader.ReadUInt());
    }

    [Fact]
    public void ServerGroupLootRuleValidationResult_WritesMappedFields()
    {
        var packet = new ServerGroupLootRuleValidationResult
        {
            GroupId   = 0x8877665544332211ul,
            Unknown0  = 0xAABBCCDDu,
            Result    = GroupActionResult.ChangeSettingsSuccess,
            Unknown1  = 0x01020304u
        };

        byte[] packetData = WritePacket(packet.Write);

        Assert.Equal(0x18, packetData.Length);
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x8877665544332211ul, reader.ReadULong());
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal((uint)GroupActionResult.ChangeSettingsSuccess, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void ServerGroupRosterUpdate_MatchesMemberStatUpdateWireSize()
    {
        var statUpdate = CreateSampleStatUpdate<ServerGroupMemberStatUpdate>();
        var rosterUpdate = CreateSampleStatUpdate<ServerGroupRosterUpdate>();

        Assert.Equal(WritePacket(statUpdate), WritePacket(rosterUpdate));
        Assert.Equal(0x60, WritePacket(rosterUpdate).Length);
    }

    [Fact]
    public void ServerGroupMemberRoleChange_WritesMappedFields()
    {
        var packet = new ServerGroupMemberRoleChange
        {
            GroupId        = 0x0102030405060708ul,
            MemberIndex    = 0x090A0B0Cu,
            TargetedPlayer = new Identity { RealmId = 1, Id = 202ul },
            ChangedFlags   = GroupMemberInfoFlags.Healer,
            IsFromPromotion = false
        };

        byte[] packetData = WritePacket(packet.Write);

        Assert.Equal(0x28, packetData.Length);
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x090A0B0Cu, reader.ReadUInt());
        Assert.Equal(1, reader.ReadUShort(14u));
        Assert.Equal(202ul, reader.ReadULong());
        Assert.Equal((uint)GroupMemberInfoFlags.Healer, reader.ReadUInt());
        Assert.False(reader.ReadBit());
    }

    [Fact]
    public void ServerQuestShareResult_WritesMappedFields()
    {
        var packet = new ServerQuestShareResult
        {
            QuestId      = 0x1234,
            Accepted     = true,
            SharerUnitId = 0xAABBCCDDu
        };

        byte[] packetData = WritePacket(packet.Write);

        Assert.Equal(0x10, packetData.Length);
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x1234, reader.ReadUShort(15u));
        Assert.True(reader.ReadBit());
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
    }

    [Fact]
    public void ServerGroupInstanceDifficultyResponse_WritesMappedFields()
    {
        var packet = new ServerGroupInstanceDifficultyResponse
        {
            GroupId       = 0x1122334455667788ul,
            CharacterGuid = 0xAABBCCDDu,
            Difficulty    = WorldDifficulty.Veteran,
            Unknown0      = 0x01020304u
        };

        byte[] packetData = WritePacket(packet.Write);

        Assert.Equal(0x18, packetData.Length);
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal((uint)WorldDifficulty.Veteran, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
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

    private static T CreateSampleStatUpdate<T>() where T : ServerGroupMemberStatUpdate, new()
    {
        return new T
        {
            GroupId        = 0x0102030405060708ul,
            TargetPlayer   = new Identity { RealmId = 1, Id = 99ul },
            Level          = 50,
            EffectiveLevel = 50,
            Unk1           = 0,
            GroupMemberId  = 7,
            Health         = 100f,
            HealthMax      = 100f,
            Shield         = 0f,
            ShieldMax      = 0f,
            Absorption     = 0f,
            AbsorptionMax  = 0f,
            Mana           = 100f,
            ManaMax        = 100f,
            HealingAbsorb  = 0f,
            HealingAbsorbMax = 0f,
            PhaseFlags1    = 1,
            PhaseFlags2    = 2,
            Path           = NexusForever.Game.Static.PlayerPath.Path.Soldier
        };
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
