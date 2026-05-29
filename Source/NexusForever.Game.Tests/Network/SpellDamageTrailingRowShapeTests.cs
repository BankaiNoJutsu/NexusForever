using NexusForever.Game.Static.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

/// <summary>
/// Wire-shape tests for <see cref="ServerSpellEffectDamage.TrailingStructure"/> rows parsed by
/// client <c>SpellDamageTrailingRow_ReadPayload</c> @ <c>1400945e0</c>.
/// </summary>
public class SpellDamageTrailingRowShapeTests
{
    [Fact]
    public void TrailingStructure_WritesSevenUInt32sAndThreeBitDamageType()
    {
        var row = new ServerSpellEffectDamage.TrailingStructure
        {
            RawDamage          = 0x01020304u,
            RawScaledDamage    = 0x11121314u,
            AbsorbedAmount     = 0x21222324u,
            ShieldAbsorbAmount = 0x31323334u,
            AdjustedDamage     = 0x41424344u,
            OverkillAmount     = 0x51525354u,
            GlanceAmount       = 0x61626364u,
            DamageType         = DamageType.Tech
        };

        byte[] data = WritePacket(row);

        using var reader = new GamePacketReader(new MemoryStream(data));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.Equal(0x61626364u, reader.ReadUInt());
        Assert.Equal(DamageType.Tech, reader.ReadEnum<DamageType>(3u));
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void DamageDescription_WritesZeroTrailingCountByDefault()
    {
        byte[] data = WritePacket(new ServerSpellEffectDamage.DamageDescription
        {
            RawDamage      = 1u,
            AdjustedDamage = 1u,
            CombatResult   = CombatResult.Hit,
            DamageType     = DamageType.Physical
        });

        using var reader = new GamePacketReader(new MemoryStream(data));
        reader.ReadUInt(); // RawDamage
        reader.ReadUInt(); // RawScaledDamage
        reader.ReadUInt(); // AbsorbedAmount
        reader.ReadUInt(); // ShieldAbsorbAmount
        reader.ReadUInt(); // AdjustedDamage
        reader.ReadUInt(); // OverkillAmount
        reader.ReadUInt(); // GlanceAmount
        reader.ReadBit();  // KilledTarget
        reader.ReadEnum<CombatResult>(4u);
        reader.ReadEnum<DamageType>(3u);
        Assert.Equal((byte)0, reader.ReadByte(8u));
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
