using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opaque spell follow-up packet carrying a concrete Spell4 id and one event-like byte.
    /// The byte remains unnamed until client parse or sniff evidence identifies the event class.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellEventByte)]
    public class ServerSpellEventByte : IWritable
    {
        public uint Spell4Id { get; set; }
        public byte Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
            writer.Write(Unknown0);
        }
    }
}
