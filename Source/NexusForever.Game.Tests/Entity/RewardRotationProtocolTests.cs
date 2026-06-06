using System;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class RewardRotationProtocolTests
{
    [Fact]
    public void ServerRewardRotationScheduleArray_WriteMatchesNativeApplyFieldOrder()
    {
        var packet = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    RewardKeyId = 0x89ABCDEFu,
                    ContentId = 0x1234u,
                    Duration = 1.5f,
                    RewardType = 3,
                    Value = 0x10203040u
                }
            }
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x89ABCDEFu, reader.ReadUInt());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(1.5f, reader.ReadSingle());
        Assert.Equal((byte)3, reader.ReadByte());
        Assert.Equal(0x10203040u, reader.ReadUInt());
    }

    [Theory]
    [InlineData(0x4000u)]
    [InlineData(0x5000u)]
    public void ServerRewardRotationScheduleArray_WriteRejectsContentIdsOutside14BitRange(uint contentId)
    {
        var packet = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    ContentId = contentId,
                    RewardKeyId = 1u,
                    Duration = 1f,
                    RewardType = 1,
                    Value = 2u
                }
            }
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("14-bit wire limit", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void ServerRewardRotationScheduleArray_WriteRejectsUnsupportedRewardTypes(byte rewardType)
    {
        var packet = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    RewardKeyId = 1u,
                    ContentId = 12u,
                    Duration = 1f,
                    RewardType = rewardType,
                    Value = 2u
                }
            }
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("reward type", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationScheduleArray_WriteRejectsNonFiniteDurations()
    {
        var packet = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    RewardKeyId = 1u,
                    ContentId = 12u,
                    Duration = float.NaN,
                    RewardType = 1,
                    Value = 2u
                }
            }
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("must be finite", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationScheduleArray_WriteRejectsNullRows()
    {
        var packet = new ServerRewardRotationScheduleArray();
        packet.Entries.Add(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("entry 0 is null", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationEntryStateUpdate_WriteMatchesDecodedEntryStateRowFormat()
    {
        var packet = new ServerRewardRotationEntryStateUpdate
        {
            TypeId = 5u,
            ContentId = 0x10203040u,
            RewardTypeId = 0x55667788u,
            State = 2,
            Value = 0xCAFEBABEu
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(5u, reader.ReadUInt(3u));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal((byte)2, reader.ReadByte());
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
    }

    [Fact]
    public void RewardRotationEntryStatePackets_WriteRejectTypeIdsOutsideThreeBitRange()
    {
        IWritable[] packets =
        [
            new ServerRewardRotationEntryStateArray
            {
                Entries =
                {
                    new ServerRewardRotationEntryStateArray.EntryStateRow
                    {
                        TypeId = 8u,
                        ContentId = 1u,
                        RewardTypeId = 2u,
                        State = 3,
                        Value = 4u
                    }
                }
            },
            new ServerRewardRotationEntryStateUpsert
            {
                TypeId = 8u,
                ContentId = 1u,
                RewardTypeId = 2u,
                State = 3,
                Value = 4u
            },
            new ServerRewardRotationEntryStateUpdate
            {
                TypeId = 8u,
                ContentId = 1u,
                RewardTypeId = 2u,
                State = 3,
                Value = 4u
            },
            new ServerRewardRotationEntryStateRemove
            {
                TypeId = 8u,
                ContentId = 1u,
                RewardTypeId = 2u,
                State = 3,
                Value = 4u
            }
        ];

        foreach (IWritable packet in packets)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

            Assert.Contains("3-bit wire limit", exception.Message);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void RewardRotationEntryStatePackets_WriteRejectInvalidRewardTypeLanes(byte rewardTypeLane)
    {
        IWritable[] packets =
        [
            new ServerRewardRotationEntryStateArray
            {
                Entries =
                {
                    new ServerRewardRotationEntryStateArray.EntryStateRow
                    {
                        TypeId = 1u,
                        ContentId = 1u,
                        RewardTypeId = 2u,
                        State = rewardTypeLane,
                        Value = 4u
                    }
                }
            },
            new ServerRewardRotationEntryStateUpsert
            {
                TypeId = 1u,
                ContentId = 1u,
                RewardTypeId = 2u,
                State = rewardTypeLane,
                Value = 4u
            }
        ];

        foreach (IWritable packet in packets)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

            Assert.Contains("state/reward-type lane", exception.Message);
        }
    }

    [Fact]
    public void ServerRewardRotationEntryStateArray_WriteRejectsNullRows()
    {
        var packet = new ServerRewardRotationEntryStateArray();
        packet.Entries.Add(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("entry 0 is null", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationContentContext_WriteMatchesDecodedFieldOrder()
    {
        var packet = new ServerRewardRotationContentContext
        {
            RewardRotationIndex = 4u,
            UInt0 = 0x11111111u,
            UInt1 = 0x22222222u,
            UInt3 = 0x44444444u,
            ContentIds = { 0x33333331u, 0x33333332u },
            Flag = true
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(4u, reader.ReadUInt(14u));
        Assert.Equal(0x11111111u, reader.ReadUInt());
        Assert.Equal(0x22222222u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x44444444u, reader.ReadUInt());
        Assert.Equal(0x33333331u, reader.ReadUInt());
        Assert.Equal(0x33333332u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
    }

    [Theory]
    [InlineData(7u)]
    [InlineData(99u)]
    public void ServerRewardRotationContentContext_WriteRejectsRewardRotationIndicesOutsideSupportedRange(uint rewardRotationIndex)
    {
        var packet = new ServerRewardRotationContentContext
        {
            RewardRotationIndex = rewardRotationIndex
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("reward rotation index", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationContentContext_WriteRejectsContentIdCountsAboveWireBudget()
    {
        var packet = new ServerRewardRotationContentContext
        {
            ContentIds = { 1u, 2u, 3u, 4u, 5u, 6u }
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("wire budget", exception.Message);
    }

    [Fact]
    public void ServerRewardRotationContentContextArray_WriteMatchesCountPlusRowLayout()
    {
        var packet = new ServerRewardRotationContentContextArray
        {
            Entries =
            {
                new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = 1u,
                    ContentIds = { 10u, 11u }
                },
                new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = 1u,
                    ContentIds = { 12u, 13u, 14u }
                }
            }
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt(14u));
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.False(reader.ReadBit());
        Assert.Equal(1u, reader.ReadUInt(14u));
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(12u, reader.ReadUInt());
        Assert.Equal(13u, reader.ReadUInt());
        Assert.Equal(14u, reader.ReadUInt());
        Assert.False(reader.ReadBit());
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
