using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell threshold update follow-up carrying a Spell4 id plus one stage byte.
    /// Client consumer: <c>SpellThreshold_HandleUpdate</c> @ <c>1403bea90</c>; reader <c>ServerSpellThresholdUpdate_ReadPayload</c> @ <c>140095fb0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellThresholdUpdate)]
    public class ServerSpellThresholdUpdate : IWritable
    {
        public uint Spell4Id { get; set; }
        public byte Stage { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
            writer.Write(Stage);
        }
    }
}
