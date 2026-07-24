using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell broadcast follow-up for opcode <c>0x07F6</c>.
    /// Client registration in <c>Network_RegisterServerOpcode_0351</c> @ <c>14006c290</c> binds
    /// opcode <c>0x07F6</c> to reader <c>ServerSpellEffectDamage_ReadPayload</c> @ <c>140095910</c>
    /// with a <c>0x48</c>-byte parsed object and no dedicated post-read apply handler.
    /// Retail apply is indirect: <c>WorldSocket_ProcessServerMessage</c> handler
    /// <c>vtable+0x58</c> -> <c>Entity_DispatchHighRangeMessage</c> case
    /// <c>1403ee268</c> -> <c>SpellWrapper_ApplyServerEffectDamage</c> ->
    /// <c>SpellWrapper_DispatchEffectDamageCombatLog</c> (combat-log floaters).
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellEffectDamage)]
    public class ServerSpellEffectDamage : IWritable
    {
        /// <summary>
        /// One trailing damage row read by <c>SpellDamageTrailingRow_ReadPayload</c> @ <c>1400945e0</c>.
        /// Wire shape mirrors the seven primary <see cref="DamageDescription"/> uint32 fields plus a
        /// 3-bit tail field. Per-row gameplay meaning (distribution splits, nested targets, etc.)
        /// remains blocked without a mapped consumer past the reader.
        /// </summary>
        public class TrailingStructure : IWritable
        {
            public uint RawDamage { get; set; }
            public uint RawScaledDamage { get; set; }
            public uint AbsorbedAmount { get; set; }
            public uint ShieldAbsorbAmount { get; set; }
            public uint AdjustedDamage { get; set; }
            public uint OverkillAmount { get; set; }
            public uint GlanceAmount { get; set; }
            /// <summary>
            /// 3-bit field at wire offset <c>0x1C</c> after the seven uint32 values.
            /// Uses the same width as <see cref="DamageDescription.DamageType"/>; retail may use this for
            /// per-row school/type on multi-hit tails, but no mapped consumer was recovered past
            /// <c>SpellDamageTrailingRow_ReadPayload</c> @ <c>1400945e0</c>. Emit <c>Physical</c> or keep the list empty.
            /// </summary>
            public DamageType DamageType { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(RawDamage);
                writer.Write(RawScaledDamage);
                writer.Write(AbsorbedAmount);
                writer.Write(ShieldAbsorbAmount);
                writer.Write(AdjustedDamage);
                writer.Write(OverkillAmount);
                writer.Write(GlanceAmount);
                writer.Write(DamageType, 3u);
            }
        }

        public class DamageDescription : IWritable // same used for 0x07F4
        {
            public uint RawDamage { get; set; }
            public uint RawScaledDamage { get; set; }
            public uint AbsorbedAmount { get; set; }
            public uint ShieldAbsorbAmount { get; set; }
            public uint AdjustedDamage { get; set; }
            public uint OverkillAmount { get; set; }
            /// <summary>
            /// Native reader SpellDamageDescription_ReadPayload places this uint32 immediately after overkill.
            /// Adjacent combat-log payloads use the same overkill-plus-glance ordering, so treat this as glance amount.
            /// Runtime spell damage producers do not yet track glance damage and currently emit zero.
            /// </summary>
            public uint GlanceAmount { get; set; }
            public bool KilledTarget { get; set; }
            public CombatResult CombatResult { get; set; }
            public DamageType DamageType { get; set; }

            public List<TrailingStructure> TrailingStructures { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(RawDamage);
                writer.Write(RawScaledDamage);
                writer.Write(AbsorbedAmount);
                writer.Write(ShieldAbsorbAmount);
                writer.Write(AdjustedDamage);
                writer.Write(OverkillAmount);
                writer.Write(GlanceAmount);
                writer.Write(KilledTarget);
                writer.Write(CombatResult, 4u);
                writer.Write(DamageType, 3u);
                
                writer.Write(TrailingStructures.Count, 8u);
                TrailingStructures.ForEach(u => u.Write(writer));
            }
        }

        public uint ServerUniqueId { get; set; }
        public uint Spell4EffectId { get; set; } = 0;
        public uint UnitId { get; set; } = 0;
        public uint TargetId { get; set; } = 0;

        public DamageDescription DamageDescriptionData { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ServerUniqueId);
            writer.Write(Spell4EffectId, 19);
            writer.Write(UnitId);
            writer.Write(TargetId);
            DamageDescriptionData.Write(writer);
        }
    }
}
