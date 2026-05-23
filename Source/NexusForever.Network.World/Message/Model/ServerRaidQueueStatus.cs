using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Raid queue status update (0x0718, 0x1A bytes).
    /// Decomp (0x14008bf80): uint64 + 15-bit uint32 + uint64 + uint32 + uint32.
    /// Field semantics remain blocked pending a non-zero retail capture or client consumer mapping.
    /// </summary>
    [Message(GameMessageOpcode.ServerRaidQueueStatus)]
    public class ServerRaidQueueStatus : IWritable
    {
        public ulong Unknown0 { get; set; }

        public uint Unknown1 { get; set; }

        public ulong Unknown2 { get; set; }

        public uint Unknown3 { get; set; }

        public uint Unknown4 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unknown0);
            writer.Write(Unknown1, 15u);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
        }
    }
}
