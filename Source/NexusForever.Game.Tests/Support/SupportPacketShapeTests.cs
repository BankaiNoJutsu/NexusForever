using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Support;

namespace NexusForever.Game.Tests.Support;

public class SupportPacketShapeTests
{
    public static TheoryData<IWritable, int> ServerSupportMappedPacketCases()
    {
        return new TheoryData<IWritable, int>
        {
            { new ServerSupportUInt32HeaderAndTriplets(), 0x30 },
            { new ServerSupportEmptyAck349(), 0 },
            { new ServerSupportUInt32Ack34A { Value = 0xAABBCCDDu }, 4 },
            { new ServerSupportEmptyAck34B(), 0 },
            { new ServerSupportUInt5AndSixUInt32(), 0x1C },
            { new ServerSupportUInt32HeaderAndFourTriplets(), 0x40 },
            { new ServerSupportUInt32AndFlags(), 8 },
            { new ServerSupportUInt32Ack350 { Value = 0x11223344u }, 4 },
            { new ServerSupportUInt32AndTwoTriplets(), 0x20 },
        };
    }

    [Theory]
    [MemberData(nameof(ServerSupportMappedPacketCases))]
    public void ServerSupportMappedPackets_WriteExpectedPayloadLength(IWritable packet, int expectedLength)
    {
        byte[] packetData = WritePacket(packet);

        Assert.Equal(expectedLength, packetData.Length);
    }

    [Fact]
    public void ServerSupportUInt32AndFlags_WritesValueFlagsAndPadding()
    {
        var packet = new ServerSupportUInt32AndFlags
        {
            Value = 0x12345678u,
            Flags = 0x3u,     // max 2-bit value
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(0x12345678u, reader.ReadUInt());
        Assert.Equal(0x3u, reader.ReadUInt(2u));
        // 30-bit zero padding consumed implicitly (remaining bits in the byte are zero).
        Assert.Equal(0u, reader.ReadUInt(30u));
    }

    [Fact]
    public void ServerSupportUInt32WideString_WritesMappedFields()
    {
        var packet = new ServerSupportUInt32WideString
        {
            Value = 0x10203040u,
            Text  = "support"
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal("support", reader.ReadWideString());
    }

    [Fact]
    public void ServerSupportUInt32HeaderAndTriplets_WritesMappedFields()
    {
        var packet = new ServerSupportUInt32HeaderAndTriplets
        {
            Header0 = 1u,
            Header1 = 2u,
            Header2 = 3u,
            Row0 = { Value0 = 10u, Value1 = 11u, Value2 = 12u },
            Row1 = { Value0 = 20u, Value1 = 21u, Value2 = 22u },
            Row2 = { Value0 = 30u, Value1 = 31u, Value2 = 32u },
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(12u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
        Assert.Equal(21u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
        Assert.Equal(31u, reader.ReadUInt());
        Assert.Equal(32u, reader.ReadUInt());
    }

    [Fact]
    public void ServerSupportUInt5AndSixUInt32_WritesMappedFields()
    {
        var packet = new ServerSupportUInt5AndSixUInt32
        {
            Value0 = 0x1Au,
            Value1 = 0xA0B0C0D0u,
            Value2 = 0x01020304u,
            Value3 = 0x50607080u,
            Value4 = 0x90A0B0C0u,
            Value5 = 0xDEADBEEFu,
            Value6 = 0xCAFEBABEu,
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(0x1Au, reader.ReadUInt(5u));
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0xDEADBEEFu, reader.ReadUInt());
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt(27u));
    }

    [Fact]
    public void ServerSupportUInt32HeaderAndFourTriplets_WritesMappedFields()
    {
        var packet = new ServerSupportUInt32HeaderAndFourTriplets
        {
            Header0 = 1u,
            Header1 = 2u,
            Header2 = 3u,
            Header3 = 4u,
            Column0 = { Value0 = 10u, Value1 = 11u, Value2 = 12u },
            Column1 = { Value0 = 20u, Value1 = 21u, Value2 = 22u },
            Column2 = { Value0 = 30u, Value1 = 31u, Value2 = 32u },
            Column3 = { Value0 = 40u, Value1 = 41u, Value2 = 42u },
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(12u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
        Assert.Equal(21u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
        Assert.Equal(31u, reader.ReadUInt());
        Assert.Equal(32u, reader.ReadUInt());
        Assert.Equal(40u, reader.ReadUInt());
        Assert.Equal(41u, reader.ReadUInt());
        Assert.Equal(42u, reader.ReadUInt());
    }

    [Fact]
    public void ServerSupportSurveyList_WritesHeaderAndRows()
    {
        var packet = new ServerSupportSurveyList
        {
            Text   = "survey",
            Value0 = 1u,
            Value1 = 2u,
            Value2 = 3u,
            Value3 = 4u,
        };
        packet.Rows.Add(new ServerSupportSurveyRow
        {
            Text   = "row",
            Value0 = 0x1111222233334444ul,
            Value1 = 0x5555666677778888ul,
            Value2 = 0x9999AAAABBBBCCCCul,
            Value3 = 0xDDDDEEEEFFFF0000ul,
            Value4 = 0x1234567890ABCDEFul,
        });

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal("survey", reader.ReadWideString());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal("row", reader.ReadWideString());
        Assert.Equal(0x1111222233334444ul, reader.ReadULong());
        Assert.Equal(0x5555666677778888ul, reader.ReadULong());
        Assert.Equal(0x9999AAAABBBBCCCCul, reader.ReadULong());
        Assert.Equal(0xDDDDEEEEFFFF0000ul, reader.ReadULong());
        Assert.Equal(0x1234567890ABCDEFul, reader.ReadULong());
    }

    [Fact]
    public void ServerSupportUInt32AndTwoTriplets_WritesMappedFields()
    {
        var packet = new ServerSupportUInt32AndTwoTriplets
        {
            LeadingValue = 0x10203040u,
            Triplet0 = { Value0 = 1u, Value1 = 2u, Value2 = 3u },
            Triplet1 = { Value0 = 4u, Value1 = 5u, Value2 = 6u },
            TrailingValue = 0xA0B0C0D0u,
        };

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(6u, reader.ReadUInt());
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
    }

    private static GamePacketReader CreateReader(byte[] packetData)
    {
        return new GamePacketReader(new MemoryStream(packetData));
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
