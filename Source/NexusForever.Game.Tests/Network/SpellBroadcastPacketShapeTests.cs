using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Tests.Network;

public class SpellBroadcastPacketShapeTests
{
    [Fact]
    public void ServerSpellThresholdClear_WritesSpell4IdAndFlag()
    {
        var packet = new ServerSpellThresholdClear
        {
            Spell4Id = 0x2ABCDu,
            Unknown0 = true
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x2ABCDu, reader.ReadUInt(18u));
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ServerSpellThresholdSpell4_WritesSpell4IdOnly()
    {
        var packet = new ServerSpellThresholdSpell4
        {
            Spell4Id = 0x15F00u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x15F00u, reader.ReadUInt(18u));
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void ServerSpellThresholdStart_WritesThreeSpell4IdsAndCastingId()
    {
        var packet = new ServerSpellThresholdStart
        {
            Spell4Id = 0x10001u,
            RootSpell4Id = 0x10002u,
            ParentSpell4Id = 0x10003u,
            CastingId = 0x44556677u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x10001u, reader.ReadUInt(18u));
        Assert.Equal(0x10002u, reader.ReadUInt(18u));
        Assert.Equal(0x10003u, reader.ReadUInt(18u));
        Assert.Equal(0x44556677u, reader.ReadUInt());
    }

    [Fact]
    public void ServerSpellThresholdUpdate_WritesSpell4IdAndStageByte()
    {
        var packet = new ServerSpellThresholdUpdate
        {
            Spell4Id = 0x20u,
            Stage = 0x07
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x20u, reader.ReadUInt(18u));
        Assert.Equal(0x07, reader.ReadByte());
    }

    [Fact]
    public void ServerSpellWrapperTierEntry_WritesSlotAndTierEntry()
    {
        var packet = new ServerSpellWrapperTierEntry
        {
            SlotOrIndex = 0x33u,
            TierEntry = new ServerSpellList.TierEntry
            {
                Spell4Id = 0x40u,
                TierIndex = 2,
                SpecIndex = 1,
                ActionSetMask = 0x4u
            }
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x33u, reader.ReadUInt());
        Assert.Equal(0x40u, reader.ReadUInt());
        Assert.Equal(2, reader.ReadByte());
        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(0, reader.ReadUShort());
        Assert.Equal(0x4u, reader.ReadUInt(4u));
        Assert.Equal(0, reader.ReadByte());
    }

    [Fact]
    public void ServerSpellWrapperNodeRemove_WritesNodeEntityAndWrapperIds()
    {
        var packet = new ServerSpellWrapperNodeRemove
        {
            NodeEntityId = 0xAABBCCDDu,
            SpellWrapperId = 0x11223344u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal(0x11223344u, reader.ReadUInt());
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
