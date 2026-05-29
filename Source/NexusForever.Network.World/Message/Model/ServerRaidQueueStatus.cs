using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerRaidQueueStatus_ReadPayload</c> (<c>14008bf80</c>).
    /// Reads opcode <c>0x0718</c> as one uint64, one 15-bit uint32, one uint64, and two trailing
    /// uint32 fields. The registered object size is <c>0x20</c>, but the mapped wire payload is
    /// only <c>0x1A</c> bytes.
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
