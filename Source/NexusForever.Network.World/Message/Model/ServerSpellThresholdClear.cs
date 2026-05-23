using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell threshold clear follow-up carrying a Spell4 id plus one trailing flag.
    /// Client consumer: <c>SpellThreshold_HandleClear</c> @ <c>1403bed60</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellThresholdClear)]
    public class ServerSpellThresholdClear : IWritable
    {
        public uint Spell4Id { get; set; }
        public bool Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
            writer.Write(Unknown0);
        }
    }
}
