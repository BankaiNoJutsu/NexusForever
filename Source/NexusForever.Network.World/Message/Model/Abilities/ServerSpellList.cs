using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Abilities
{
    [Message(GameMessageOpcode.ServerSpellList)]
    public class ServerSpellList : IWritable
    {
        public class SpellEntry : IWritable
        {
            public uint Id { get; set; }
            public uint Spell4Id { get; set; }
            public uint ParentSpell4Id { get; set; }
            public uint BaseSpell4Id { get; set; }
            public bool Active { get; set; }
            public List<TierEntry> Tiers { get; } = [];
            public List<SpellEntryStateA> StateA { get; } = [];
            public List<SpellEntryStateB> StateB { get; } = [];

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Id);
                writer.Write(Spell4Id);
                writer.Write(ParentSpell4Id);
                writer.Write(BaseSpell4Id, 18u);
                writer.Write(Active);

                writer.Write((uint)Tiers.Count);
                Tiers.ForEach(t => t.Write(writer));

                writer.Write((byte)StateA.Count);
                StateA.ForEach(s => s.Write(writer));

                writer.Write((byte)StateB.Count);
                StateB.ForEach(s => s.Write(writer));
            }
        }

        public class TierEntry : IWritable
        {
            public class VariantEntry : IWritable
            {
                public uint Spell4EffectId { get; set; }
                public uint Value0 { get; set; }
                public uint Value1 { get; set; }
                public uint Value2 { get; set; }
                public byte VariantType { get; set; }

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(Spell4EffectId, 19u);
                    writer.Write(Value0);
                    writer.Write(Value1);
                    writer.Write(Value2);
                    writer.Write(VariantType, 2u);
                }
            }

            public uint Spell4Id { get; set; }
            public byte TierIndex { get; set; }
            public byte SpecIndex { get; set; }
            public ushort Flags { get; set; }
            public uint ActionSetMask { get; set; }
            public List<VariantEntry> Variants { get; } = [];

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Spell4Id);
                writer.Write(TierIndex);
                writer.Write(SpecIndex);
                writer.Write(Flags);
                writer.Write(ActionSetMask, 4u);

                writer.Write((byte)Variants.Count);
                Variants.ForEach(v => v.Write(writer));
            }
        }

        public class SpellEntryStateA : IWritable
        {
            public uint Value0 { get; set; }
            public byte Value1 { get; set; }
            public uint Value2 { get; set; }
            public uint Value3 { get; set; }
            public uint Value4 { get; set; }
            public uint Value5 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(Value1);
                writer.Write(Value2);
                writer.Write(Value3);
                writer.Write(Value4);
                writer.Write(Value5);
            }
        }

        public class SpellEntryStateB : IWritable
        {
            public ushort Value0 { get; set; }
            public uint Value1 { get; set; }
            public byte Value2 { get; set; }
            public uint Value3 { get; set; }
            public uint Value4 { get; set; }
            public uint Value5 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(Value1);
                writer.Write(Value2);
                writer.Write(Value3);
                writer.Write(Value4);
                writer.Write(Value5);
            }
        }

        public List<SpellEntry> Spells { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Spells.Count);
            Spells.ForEach(s => s.Write(writer));
        }
    }
}
