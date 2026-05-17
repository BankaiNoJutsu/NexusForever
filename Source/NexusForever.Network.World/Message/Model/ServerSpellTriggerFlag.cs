using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opaque spell follow-up packet carrying a concrete Spell4 id plus a boolean flag.
    /// Treat this as diagnostic-only until the client-side event meaning is mapped.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellTriggerFlag)]
    public class ServerSpellTriggerFlag : IWritable
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
