using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Structurally decoded matching-manager state packet carrying one boolean flag.
    /// No current server sender/consumer or direct client opcode-immediate reference has been
    /// recovered, so the flag meaning remains diagnostic-only.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingManagerFlag)]
    public class ServerMatchingManagerFlag : IWritable
    {
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Flag);
        }
    }
}