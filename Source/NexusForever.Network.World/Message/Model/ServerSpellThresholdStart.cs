using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell threshold start follow-up carrying current, root, and parent Spell4 ids plus a casting id.
    /// Client consumer: <c>SpellThreshold_HandleStart</c> @ <c>1403be940</c>; reader <c>ServerSpellThresholdStart_ReadPayload</c> @ <c>140095f30</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellThresholdStart)]
    public class ServerSpellThresholdStart : IWritable
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
