using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// Shared group-character row from <c>GroupCharacter_ReadPayload</c> (<c>140082420</c>).
    /// Reused by group invite, group member, and nested group payload readers.
    /// </summary>
    public class GroupCharacter : IWritable
    {
        public string Name { get; set; }
        public Faction Faction { get; set; }
        public Race Race { get; set; }
        public Class Class { get; set; }
        public Sex Sex { get; set; }
        public byte Level { get; set; }
        public byte EffectiveLevel { get; set; }
        public Game.Static.PlayerPath.Path Path { get; set; }
        /// <summary>
        /// 17-bit prefix in the group stat block (parsed <c>+0x1c</c> in
        /// <c>Group_CopyMemberStatBlockFromPayload</c> @ <c>140607490</c>). Semantics blocked.
        /// </summary>
        public uint StatBlockPrefix17 { get; set; }
        public ushort GroupMemberId { get; set; }

        public GroupMemberStatSlot[] StatSlots = new GroupMemberStatSlot[5];

        /// <summary>
        /// Trailing counted rows reuse <see cref="PrimeLevelInfo"/> helper <c>1400ad150</c>.
        /// </summary>
        public List<PrimeLevelInfo> PrimeLevels { get; set; } = new List<PrimeLevelInfo>();

        public Identity MentoringTarget { get; set; }

        /// <summary>
        /// Trailing <c>uint32</c> after mentoring identity (parsed <c>+0x50</c>). Semantics blocked.
        /// </summary>
        public uint Unknown10 { get; set; }

        /// <summary>
        /// Packed half-float vital pair matching <see cref="ServerGroupMemberStatUpdate"/> stat block.
        /// Serialize with <see cref="GamePacketWriter.WritePackedFloat"/> when sourcing float vitals.
        /// </summary>
        public ushort Health { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort HealthMax { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort Shield { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort ShieldMax { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort InterruptArmor { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort InterruptArmorMax { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort Absorption { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort AbsorptionMax { get; set; }

        /// <inheritdoc cref="Health"/>
        /// <remarks>Client group UI label is mana; NF group-server maps player focus into this slot.</remarks>
        public ushort Mana { get; set; }

        /// <inheritdoc cref="Mana"/>
        public ushort ManaMax { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort HealingAbsorb { get; set; }

        /// <inheritdoc cref="Health"/>
        public ushort HealingAbsorbMax { get; set; }

        public ushort Realm { get; set; }
        public ushort WorldZoneId { get; set; }
        public uint MapId { get; set; }
        public uint PhaseId { get; set; } = 1;
        public bool SyncedToGroup { get; set; }

        /// <inheritdoc cref="ServerGroupMemberStatUpdate.PhaseFlags1"/>
        public uint PhaseFlags1 { get; set; }

        /// <inheritdoc cref="ServerGroupMemberStatUpdate.PhaseFlags2"/>
        public uint PhaseFlags2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Name);
            writer.Write(Faction, 14u);
            writer.Write(Race, 14u);
            writer.Write(Class, 14u);
            writer.Write(Sex, 2u);
            writer.Write(Level, 7u);
            writer.Write(EffectiveLevel, 7u);
            writer.Write(Path, 3u);
            writer.Write(StatBlockPrefix17, 17u);
            writer.Write(GroupMemberId);

            for (var i = 0; i < 5; ++i)
            {
                StatSlots[i] = new GroupMemberStatSlot();
                StatSlots[i].Write(writer);
            }

            if (MentoringTarget == null)
            {
                writer.Write((ushort)0, 14u);
                writer.Write((ulong)0);
            }
            else
                MentoringTarget.Write(writer);

            writer.Write(Unknown10);
            writer.Write(Health);
            writer.Write(HealthMax);
            writer.Write(Shield);
            writer.Write(ShieldMax);
            writer.Write(InterruptArmor);
            writer.Write(InterruptArmorMax);
            writer.Write(Absorption);
            writer.Write(AbsorptionMax);
            writer.Write(Mana);
            writer.Write(ManaMax);
            writer.Write(HealingAbsorb);
            writer.Write(HealingAbsorbMax);
            writer.Write(Realm, 14u);
            writer.Write(WorldZoneId, 15u);
            writer.Write(MapId);
            writer.Write(PhaseId);
            writer.Write(SyncedToGroup);
            writer.Write(PhaseFlags1);
            writer.Write(PhaseFlags2);

            writer.Write(PrimeLevels.Count);
            PrimeLevels.ForEach(i => i.Write(writer));
        }
    }
}
