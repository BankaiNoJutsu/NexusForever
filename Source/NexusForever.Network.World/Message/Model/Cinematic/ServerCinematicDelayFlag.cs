using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Cinematic
{
    /// <summary>
    /// Structurally decoded cinematic setup packet carrying a 32-bit delay and one trailing flag bit.
    /// Current runtime evidence only shows the zeroed bootstrap send and the Q3673 flag-set send
    /// before transition-duration setup, so the flag meaning remains unresolved.
    /// </summary>
    [Message(GameMessageOpcode.ServerCinematicDelayFlag)]
    public class ServerCinematicDelayFlag : IWritable
    {
        public uint Delay { get; set; }
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Delay);
            writer.Write(Flag);
        }
    }
}