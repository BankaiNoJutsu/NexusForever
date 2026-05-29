using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    public class TargetInfo : IWritable
    {
        public class EffectInfo : IWritable
        {
            public class DamageDescription : IWritable // same used for 0x07F6
            {
                /// <summary>
                /// Same trailing-row wire shape as <see cref="ServerSpellEffectDamage.TrailingStructure"/>.
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

            public uint Spell4EffectId { get; set; }
            public uint EffectUniqueId { get; set; }
            public uint DelayTime { get; set; }
            public int TimeRemaining { get; set; }
            public byte InfoType { get; set; }

            public DamageDescription DamageDescriptionData { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Spell4EffectId, 19u);
                writer.Write(EffectUniqueId);
                writer.Write(DelayTime);
                writer.Write(TimeRemaining);
                writer.Write(InfoType, 2u);

                if (InfoType == 1)
                    DamageDescriptionData.Write(writer);
                else
                    writer.Write(0u, 1u);
            }
        }

        public uint UnitId { get; set; }
        public byte Ndx { get; set; }
        public byte TargetFlags { get; set; }
        public ushort InstanceCount { get; set; }
        public CombatResult CombatResult { get; set; }

        public List<EffectInfo> EffectInfoData { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(Ndx);
            writer.Write(TargetFlags);
            writer.Write(InstanceCount);
            writer.Write(CombatResult, 4u);

            writer.Write(EffectInfoData.Count, 8u);
            EffectInfoData.ForEach(u => u.Write(writer));
        }
    }
}
