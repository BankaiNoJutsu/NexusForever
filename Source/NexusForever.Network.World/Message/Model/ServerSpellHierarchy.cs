using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opaque spell hierarchy follow-up packet carrying root, parent, and current Spell4 ids.
    /// This is tracked as part of the broader 0x07F5..0x0818 spell broadcast family.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellHierarchy)]
    public class ServerSpellHierarchy : IWritable
    {
        public uint Spell4Id { get; set; }
        public uint RootSpell4Id { get; set; }
        public uint ParentSpell4Id { get; set; } = 0;
        public uint CastingId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
            writer.Write(RootSpell4Id, 18u);
            writer.Write(ParentSpell4Id, 18u);
            writer.Write(CastingId);
        }
    }
}
