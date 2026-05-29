using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Network;

public class GroupCharacterPacketShapeTests
{
    [Fact]
    public void GroupCharacter_WritesOffsetBackedTailFieldsAndPrimeLevels()
    {
        var packet = new GroupCharacter
        {
            Name = "Group Tester",
            Faction = Faction.Exile,
            Race = Race.Human,
            Class = Class.Esper,
            Sex = Sex.Female,
            Level = 50,
            EffectiveLevel = 49,
            Path = PlayerPath.Soldier,
            StatBlockPrefix17 = 0x1AAAAu,
            GroupMemberId = 7,
            Realm = 0x1234,
            WorldZoneId = 0x4567,
            MapId = 0x11223344u,
            PhaseId = 0x55667788u,
            SyncedToGroup = true,
            PrimeLevels =
            [
                new PrimeLevelInfo
                {
                    WorldId = 0x2345,
                    PrimeLevelAchieved = 0x6789
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal("Group Tester", reader.ReadWideString());
        Assert.Equal(Faction.Exile, reader.ReadEnum<Faction>(14u));
        Assert.Equal(Race.Human, reader.ReadEnum<Race>(14u));
        Assert.Equal(Class.Esper, reader.ReadEnum<Class>(14u));
        Assert.Equal(Sex.Female, reader.ReadEnum<Sex>(2u));
        Assert.Equal((byte)50, reader.ReadByte(7u));
        Assert.Equal((byte)49, reader.ReadByte(7u));
        Assert.Equal(PlayerPath.Soldier, reader.ReadEnum<PlayerPath>(3u));
        Assert.Equal(0x1AAAAu, reader.ReadUInt(17u));
        Assert.Equal((ushort)7, reader.ReadUShort());

        for (var index = 0; index < 5; index++)
        {
            Assert.Equal((ushort)0, reader.ReadUShort());
            Assert.Equal((byte)48, reader.ReadByte());
        }

        Assert.Equal((ushort)0, reader.ReadUShort(14u));
        Assert.Equal(0ul, reader.ReadULong());
        Assert.Equal(0u, reader.ReadUInt());
        for (var index = 0; index < 12; index++)
        {
            Assert.Equal((ushort)0, reader.ReadUShort());
        }
        Assert.Equal((ushort)0x1234, reader.ReadUShort(14u));
        Assert.Equal((ushort)0x4567, reader.ReadUShort(15u));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)0x2345, reader.ReadUShort(15u));
        Assert.Equal((ushort)0x6789, reader.ReadUShort());
    }

    private static byte[] WritePacket(IWritable message)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
