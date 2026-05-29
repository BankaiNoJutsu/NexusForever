using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Spell;

public class CombatLogPacketShapeTests
{
    [Fact]
    public void ServerSpellEffectDamage_WriteSerializesMappedDamageDescription()
    {
        byte[] data = WritePacket(new ServerSpellEffectDamage
        {
            ServerUniqueId = 0x01020304u,
            Spell4EffectId = 0x54321u,
            UnitId         = 0x11121314u,
            TargetId       = 0x21222324u,
            DamageDescriptionData = new ServerSpellEffectDamage.DamageDescription
            {
                RawDamage          = 0x31323334u,
                RawScaledDamage    = 0x41424344u,
                AbsorbedAmount     = 0x51525354u,
                ShieldAbsorbAmount = 0x61626364u,
                AdjustedDamage     = 0x71727374u,
                OverkillAmount     = 0x81828384u,
                GlanceAmount       = 0x91929394u,
                KilledTarget       = true,
                CombatResult       = CombatResult.Critical,
                DamageType         = DamageType.Magic
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x54321u, reader.ReadUInt(19u));
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        AssertDamageDescription(reader, 0x91929394u, CombatResult.Critical, DamageType.Magic);
    }

    [Fact]
    public void TargetInfoDamageDescription_WriteSerializesMappedEmbeddedDamageDescription()
    {
        var targetInfo = new TargetInfo
        {
            UnitId        = 0x01020304u,
            Ndx           = 0x11,
            TargetFlags   = 0x22,
            InstanceCount = 0x3344,
            CombatResult  = CombatResult.Hit
        };
        targetInfo.EffectInfoData.Add(new TargetInfo.EffectInfo
        {
            Spell4EffectId = 0x54321u,
            EffectUniqueId = 0x11121314u,
            DelayTime      = 0x21222324u,
            TimeRemaining  = -1,
            InfoType       = 1,
            DamageDescriptionData = new TargetInfo.EffectInfo.DamageDescription
            {
                RawDamage          = 0x31323334u,
                RawScaledDamage    = 0x41424344u,
                AbsorbedAmount     = 0x51525354u,
                ShieldAbsorbAmount = 0x61626364u,
                AdjustedDamage     = 0x71727374u,
                OverkillAmount     = 0x81828384u,
                GlanceAmount       = 0x91929394u,
                KilledTarget       = true,
                CombatResult       = CombatResult.Critical,
                DamageType         = DamageType.Magic
            }
        });

        byte[] data = WritePacket(targetInfo);

        using var reader = CreateReader(data);
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal((byte)0x11, reader.ReadByte());
        Assert.Equal((byte)0x22, reader.ReadByte());
        Assert.Equal((ushort)0x3344, reader.ReadUShort());
        Assert.Equal(CombatResult.Hit, reader.ReadEnum<CombatResult>(4u));
        Assert.Equal((byte)1, reader.ReadByte(8u));
        Assert.Equal(0x54321u, reader.ReadUInt(19u));
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(-1, reader.ReadInt());
        Assert.Equal((byte)1, reader.ReadByte(2u));
        AssertDamageDescription(reader, 0x91929394u, CombatResult.Critical, DamageType.Magic);
    }

    [Fact]
    public void CombatLogDamage_WriteSerializesMappedDamagePayload()
    {
        byte[] data = WritePacket(new ServerCombatLog
        {
            CombatLog = new CombatLogDamage
            {
                MitigatedDamage   = 0x01020304u,
                RawDamage         = 0x11121314u,
                Shield            = 0x21222324u,
                Absorption        = 0x31323334u,
                Overkill          = 0x41424344u,
                Glance            = 0x51525354u,
                BTargetVulnerable = true,
                BKilled           = false,
                BPeriodic         = true,
                DamageType        = DamageType.Magic,
                EffectType        = SpellEffectType.DistributedDamage,
                CastData          = CreateCastData()
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(CombatLogType.Damage, reader.ReadEnum<CombatLogType>(6u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.Equal(DamageType.Magic, reader.ReadEnum<DamageType>(3u));
        Assert.Equal(SpellEffectType.DistributedDamage, reader.ReadEnum<SpellEffectType>(8u));
        AssertCastData(reader);
    }

    [Fact]
    public void CombatLogDamageShield_WriteSerializesMappedDamageShieldPayload()
    {
        byte[] data = WritePacket(new ServerCombatLog
        {
            CombatLog = new CombatLogDamageShield
            {
                MitigatedDamage   = 0x01020304u,
                RawDamage         = 0x11121314u,
                Shield            = 0x21222324u,
                Absorption        = 0x31323334u,
                Overkill          = 0x41424344u,
                Glance            = 0x51525354u,
                BTargetVulnerable = false,
                BKilled           = true,
                BPeriodic         = true,
                DamageType        = DamageType.Tech,
                EffectType        = SpellEffectType.DamageShields,
                CastData          = CreateCastData()
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(CombatLogType.DamageShields, reader.ReadEnum<CombatLogType>(6u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.Equal(DamageType.Tech, reader.ReadEnum<DamageType>(3u));
        Assert.Equal(SpellEffectType.DamageShields, reader.ReadEnum<SpellEffectType>(8u));
        AssertCastData(reader);
    }

    [Fact]
    public void CombatLogAbsorption_WriteSerializesAmountAndCastContext()
    {
        byte[] data = WritePacket(new ServerCombatLog
        {
            CombatLog = new CombatLogAbsorption
            {
                AbsorptionAmount = 0x0E0F1011u,
                CastData         = CreateCastData()
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(CombatLogType.Absorption, reader.ReadEnum<CombatLogType>(6u));
        Assert.Equal(0x0E0F1011u, reader.ReadUInt());
        AssertCastData(reader);
    }

    [Fact]
    public void CombatLogHeal_WriteSerializesMappedHealPayload()
    {
        byte[] data = WritePacket(new ServerCombatLog
        {
            CombatLog = new CombatLogHeal
            {
                HealAmount = 0x01020304u,
                Overheal   = 0x11121314u,
                Absorption = 0x21222324u,
                EffectType = SpellEffectType.HealShields,
                CastData   = CreateCastData()
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(CombatLogType.Heal, reader.ReadEnum<CombatLogType>(6u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(SpellEffectType.HealShields, reader.ReadEnum<SpellEffectType>(8u));
        AssertCastData(reader);
    }

    [Fact]
    public void CombatLogHealingAbsorption_WriteSerializesAmountAndCastContext()
    {
        byte[] data = WritePacket(new ServerCombatLog
        {
            CombatLog = new CombatLogHealingAbsorption
            {
                Amount   = 0x0A0B0C0Du,
                CastData = CreateCastData()
            }
        });

        using var reader = CreateReader(data);
        Assert.Equal(CombatLogType.HealingAbsorption, reader.ReadEnum<CombatLogType>(6u));
        Assert.Equal(0x0A0B0C0Du, reader.ReadUInt());
        AssertCastData(reader);
    }

    [Theory]
    [InlineData(1000u, 6000u, true)]
    [InlineData(1000u, 0u, false)]
    [InlineData(0u, 6000u, false)]
    public void IsPeriodicDamageLog_OnlyMarksBoundedTickRows(uint tickTime, uint durationTime, bool expected)
    {
        var entry = new Spell4EffectsEntry
        {
            TickTime     = tickTime,
            DurationTime = durationTime
        };

        Assert.Equal(expected, SpellHandler.IsPeriodicDamageLog(entry));
    }

    private static CombatLogCastData CreateCastData()
    {
        return new CombatLogCastData
        {
            CasterId     = 0x61626364u,
            TargetId     = 0x71727374u,
            SpellId      = 0x23456u,
            CombatResult = CombatResult.Critical
        };
    }

    private static void AssertCastData(GamePacketReader reader)
    {
        Assert.Equal(0x61626364u, reader.ReadUInt());
        Assert.Equal(0x71727374u, reader.ReadUInt());
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal(CombatResult.Critical, reader.ReadEnum<CombatResult>(4u));
    }

    private static void AssertDamageDescription(GamePacketReader reader, uint glanceAmount, CombatResult combatResult, DamageType damageType)
    {
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.Equal(0x61626364u, reader.ReadUInt());
        Assert.Equal(0x71727374u, reader.ReadUInt());
        Assert.Equal(0x81828384u, reader.ReadUInt());
        Assert.Equal(glanceAmount, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(combatResult, reader.ReadEnum<CombatResult>(4u));
        Assert.Equal(damageType, reader.ReadEnum<DamageType>(3u));
        Assert.Equal((byte)0, reader.ReadByte(8u));
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
