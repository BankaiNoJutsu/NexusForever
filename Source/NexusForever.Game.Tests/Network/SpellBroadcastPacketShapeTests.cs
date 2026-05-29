using NexusForever.Game.Static.Spell;
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

    [Fact]
    public void ServerSpellEffectNestedTargets_WritesDamageDescriptionList()
    {
        var packet = new ServerSpellEffectNestedTargets
        {
            ServerUniqueId = 0x01020304u,
            Spell4EffectId = 0x54321u,
            TargetId       = 0x11121314u,
            DamageDescriptions =
            [
                new ServerSpellEffectDamage.DamageDescription
                {
                    RawDamage          = 0x21222324u,
                    RawScaledDamage    = 0x31323334u,
                    AbsorbedAmount     = 0x41424344u,
                    ShieldAbsorbAmount = 0x51525354u,
                    AdjustedDamage     = 0x61626364u,
                    OverkillAmount     = 0x71727374u,
                    GlanceAmount       = 0x81828384u,
                    KilledTarget       = true,
                    CombatResult       = CombatResult.NeedsHitResultCalc,
                    DamageType         = DamageType.HealShields,
                    TrailingStructures =
                    [
                        new ServerSpellEffectDamage.TrailingStructure
                        {
                            RawDamage          = 0x91929394u,
                            RawScaledDamage    = 0xA1A2A3A4u,
                            AbsorbedAmount     = 0xB1B2B3B4u,
                            ShieldAbsorbAmount = 0xC1C2C3C4u,
                            AdjustedDamage     = 0xD1D2D3D4u,
                            OverkillAmount     = 0xE1E2E3E4u,
                            GlanceAmount       = 0xF1F2F3F4u,
                            DamageType         = DamageType.Magic
                        }
                    ]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x54321u, reader.ReadUInt(19u));
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal((byte)1, reader.ReadByte(8u));

        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.Equal(0x61626364u, reader.ReadUInt());
        Assert.Equal(0x71727374u, reader.ReadUInt());
        Assert.Equal(0x81828384u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(CombatResult.NeedsHitResultCalc, reader.ReadEnum<CombatResult>(4u));
        Assert.Equal(DamageType.HealShields, reader.ReadEnum<DamageType>(3u));
        Assert.Equal((byte)1, reader.ReadByte(8u));

        Assert.Equal(0x91929394u, reader.ReadUInt());
        Assert.Equal(0xA1A2A3A4u, reader.ReadUInt());
        Assert.Equal(0xB1B2B3B4u, reader.ReadUInt());
        Assert.Equal(0xC1C2C3C4u, reader.ReadUInt());
        Assert.Equal(0xD1D2D3D4u, reader.ReadUInt());
        Assert.Equal(0xE1E2E3E4u, reader.ReadUInt());
        Assert.Equal(0xF1F2F3F4u, reader.ReadUInt());
        Assert.Equal(DamageType.Magic, reader.ReadEnum<DamageType>(3u));
        Assert.Equal(0u, reader.BytesRemaining);
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
