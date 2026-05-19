using System;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class RewardRotationProtocolTests
{
    [Fact]
    public void ServerRewardRotationScheduleArray_WriteUsesRewardKeyIdBeforeContentId()
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
    [InlineData(uint.MaxValue)]
    public void ServerRewardRotationScheduleArray_WriteRejectsContentIdsOutside14BitRange(uint contentId)
    {
        var packet = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    RewardKeyId = 1u,
                    ContentId = contentId,
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

    [Fact]
    public void ServerRewardRotationEntryStateArray_WriteRejectsNullRows()
    {
        var packet = new ServerRewardRotationEntryStateArray();
        packet.Entries.Add(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("entry 0 is null", exception.Message);
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
