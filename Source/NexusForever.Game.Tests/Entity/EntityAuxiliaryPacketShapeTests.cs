using System.Numerics;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Entity;

public class EntityAuxiliaryPacketShapeTests
{
    [Fact]
    public void Server0x025F_WriteSerializesDecodedScalarAndTripleFields()
    {
        byte[] data = WritePacket(new Server0x025F
        {
            Value0 = 0x1234,
            Value1 = 0x20304050u,
            Value2 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 0x01020304u,
                Value1 = 0x05060708u,
                Value2 = 0x090A0B0Cu
            },
            Value5 = 0x60708090u,
            Value6 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 0x11121314u,
                Value1 = 0x15161718u,
                Value2 = 0x191A1B1Cu
            },
            Value9 = 0xA0B0C0D0u,
            Value10 = 0x0D0C0B0Au
        });

        using var reader = CreateReader(data);
        Assert.Equal((ushort)0x1234, reader.ReadUShort(16u));
        Assert.Equal(0x20304050u, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x05060708u, reader.ReadUInt());
        Assert.Equal(0x090A0B0Cu, reader.ReadUInt());
        Assert.Equal(0x60708090u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x15161718u, reader.ReadUInt());
        Assert.Equal(0x191A1B1Cu, reader.ReadUInt());
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        Assert.Equal(0x0D0C0B0Au, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0260_WriteSerializesDecodedRowList()
    {
        var packet = new Server0x0260();
        packet.Rows.Add(new Server0x025FRow
        {
            Value0 = 0x2222,
            Value1 = 0x33333333u,
            Value2 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 1u,
                Value1 = 2u,
                Value2 = 3u
            },
            Value5 = 4u,
            Value6 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 5u,
                Value1 = 6u,
                Value2 = 7u
            },
            Value9 = 8u,
            Value10 = 9u
        });

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)0x2222, reader.ReadUShort(16u));
        Assert.Equal(0x33333333u, reader.ReadUInt());
        for (uint expected = 1u; expected <= 9u; expected++)
            Assert.Equal(expected, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0263_WriteSerializesDecodedBitPackedRow()
    {
        byte[] data = WritePacket(new Server0x0263
        {
            Value0 = 0x10203040u,
            Value1 = 0x50607080u,
            Value2 = 0x12345u,
            Value3 = 0x0FEDCu,
            Value4 = 0x11111u,
            Value5 = 0x90A0B0C0u,
            Value6 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 0x01010101u,
                Value1 = 0x02020202u,
                Value2 = 0x03030303u
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x12345u, reader.ReadUInt(17u));
        Assert.Equal(0x0FEDCu, reader.ReadUInt(17u));
        Assert.Equal(0x11111u, reader.ReadUInt(17u));
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0x01010101u, reader.ReadUInt());
        Assert.Equal(0x02020202u, reader.ReadUInt());
        Assert.Equal(0x03030303u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0261_WriteSerializesDecodedBitPackedRows()
    {
        var packet = new Server0x0261();
        packet.Rows.Add(new Server0x0263Row
        {
            Value0 = 1u,
            Value1 = 2u,
            Value2 = 3u,
            Value3 = 4u,
            Value4 = 5u,
            Value5 = 6u,
            Value6 = new ServerUnresolvedUInt32Triple
            {
                Value0 = 7u,
                Value1 = 8u,
                Value2 = 9u
            }
        });

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt(17u));
        Assert.Equal(4u, reader.ReadUInt(17u));
        Assert.Equal(5u, reader.ReadUInt(17u));
        Assert.Equal(6u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(8u, reader.ReadUInt());
        Assert.Equal(9u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0264_WriteSerializesDecodedScalarAndValueList()
    {
        var packet = new Server0x0264
        {
            Value0 = 0x10203040u,
            Value1 = 0x1234,
            Value2 = 0x50607080u
        };
        packet.Values.Add(0x90A0B0C0u);
        packet.Values.Add(0xD0C0B0A0u);

        using var reader = CreateReader(WritePacket(packet));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal((ushort)0x1234, reader.ReadUShort(16u));
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0xD0C0B0A0u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0889_WriteSerializesDecodedUInt32Triplet()
    {
        using var reader = CreateReader(WritePacket(new Server0x0889
        {
            Value0 = 0x10203040u,
            Value1 = 0x50607080u,
            Value2 = 0x90A0B0C0u
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x08CC_WriteSerializesDecodedUInt32AndWideString()
    {
        using var reader = CreateReader(WritePacket(new Server0x08CC
        {
            Value = 0x10203040u,
            Text = "entity text"
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal("entity text", reader.ReadWideString());
    }

    [Fact]
    public void Server0x08F4_WriteSerializesDecodedUInt32UInt5UInt32()
    {
        using var reader = CreateReader(WritePacket(new Server0x08F4
        {
            Value0 = 0x10203040u,
            Value1 = 0x1Au,
            Value2 = 0x50607080u
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x1Au, reader.ReadUInt(5u));
        Assert.Equal(0x50607080u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x0939_WriteSerializesDecodedUInt32UInt14UInt18AndWideString()
    {
        using var reader = CreateReader(WritePacket(new Server0x0939
        {
            Value0 = 0x10203040u,
            Value1 = 0x1234u,
            Value2 = 0x23456u,
            Text = "entity label"
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal("entity label", reader.ReadWideString());
    }

    [Fact]
    public void Server0x093D_WriteSerializesDecodedUInt32UInt5Pair()
    {
        using var reader = CreateReader(WritePacket(new Server0x093D
        {
            Value0 = 0x10203040u,
            Value1 = 0x1Bu,
            Value2 = 0x50607080u,
            Value3 = 0x90A0B0C0u
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x1Bu, reader.ReadUInt(5u));
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
    }

    [Fact]
    public void Server0x093E_WriteSerializesDecodedTwoUInt32AndUInt64()
    {
        using var reader = CreateReader(WritePacket(new Server0x093E
        {
            Value0 = 0x10203040u,
            Value1 = 0x50607080u,
            Value2 = 0x0102030405060708ul
        }));

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
    }

    [Fact]
    public void EntityThreatAndTargetPackets_WriteMappedPayloads()
    {
        using (var reader = CreateReader(WritePacket(new ServerEntityTargetUnit
        {
            UnitId = 0x10203040u,
            NewTargetId = 0x50607080u,
            ThreatLevel = 0x90A0B0C0u
        })))
        {
            Assert.Equal(0x10203040u, reader.ReadUInt());
            Assert.Equal(0x50607080u, reader.ReadUInt());
            Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        }

        using (var reader = CreateReader(WritePacket(new ServerEntityThreatUpdate
        {
            UnitId = 1u,
            TargetId = 2u,
            ThreatLevel = 3u
        })))
        {
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(3u, reader.ReadUInt());
        }

        using (var reader = CreateReader(WritePacket(new ServerEntityThreatListUpdate
        {
            SrcUnitId = 10u,
            ThreatUnitIds = [11u, 12u, 13u, 14u, 15u],
            ThreatLevels = [21u, 22u, 23u, 24u, 25u]
        })))
        {
            Assert.Equal(10u, reader.ReadUInt());
            for (uint expected = 11u; expected <= 15u; expected++)
                Assert.Equal(expected, reader.ReadUInt());
            for (uint expected = 21u; expected <= 25u; expected++)
                Assert.Equal(expected, reader.ReadUInt());
        }
    }

    [Fact]
    public void EntityStatPackets_WriteMappedPayloads()
    {
        using (var reader = CreateReader(WritePacket(new ServerEntityStatUpdateFloat
        {
            UnitId = 0x10203040u,
            Stat = new StatValueUpdate
            {
                Stat = Stat.Focus,
                Type = StatType.Float,
                Value = 123.5f
            }
        })))
        {
            Assert.Equal(0x10203040u, reader.ReadUInt());
            Assert.Equal(Stat.Focus, reader.ReadEnum<Stat>(5u));
            Assert.Equal(123.5f, reader.ReadSingle());
        }

        using (var reader = CreateReader(WritePacket(new ServerEntityStatUpdateInteger
        {
            UnitId = 0x50607080u,
            Stat = new StatValueUpdate
            {
                Stat = Stat.Health,
                Type = StatType.Integer,
                Value = 321f
            }
        })))
        {
            Assert.Equal(0x50607080u, reader.ReadUInt());
            Assert.Equal(Stat.Health, reader.ReadEnum<Stat>(5u));
            Assert.Equal(321u, reader.ReadUInt());
        }
    }

    [Fact]
    public void MapTrackedUnitPackets_WriteMappedPayloads()
    {
        using (var reader = CreateReader(WritePacket(new ServerMapTrackedUnitUpdate
        {
            TrackedUnitId = 0x10203040u,
            Position = new Vector3(1.25f, 2.5f, 3.75f),
            TrackingSlotId = 0x1234u
        })))
        {
            Assert.Equal(0x10203040u, reader.ReadUInt());
            Assert.Equal(1.25f, reader.ReadSingle());
            Assert.Equal(2.5f, reader.ReadSingle());
            Assert.Equal(3.75f, reader.ReadSingle());
            Assert.Equal(0x1234u, reader.ReadUInt(15u));
        }

        using (var reader = CreateReader(WritePacket(new ServerMapTrackedUnitDisable
        {
            TrackedUnitId = 0x50607080u
        })))
        {
            Assert.Equal(0x50607080u, reader.ReadUInt());
        }
    }

    private static GamePacketReader CreateReader(byte[] data)
    {
        return new GamePacketReader(new MemoryStream(data));
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
