using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Unresolved spell follow-up packet carrying only one Spell4 id.
    /// Current decompile evidence confirms the 18-bit payload shape but not the event semantics.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpell0815)]
    public class ServerSpell0815 : IWritable
    {
        public uint Spell4Id { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
        }
    }
}