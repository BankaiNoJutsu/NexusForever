using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell threshold follow-up carrying only one Spell4 id.
    /// Client consumer: <c>SpellThreshold_HandleClear</c> @ <c>1403bed60</c>; reader <c>ServerUInt18_ReadPayload</c> @ <c>140080d30</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellThresholdSpell4)]
    public class ServerSpellThresholdSpell4 : IWritable
    {
        public uint Spell4Id { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
        }
    }
}
