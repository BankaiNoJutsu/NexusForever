using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Raid queue status update (0x0718, 0x1A bytes).
    /// Decomp (0x14008bf80): uint64 + 15-bit + uint64 + 4 bytes + uint32.
    /// </summary>
    [Message(GameMessageOpcode.ServerRaidQueueStatus)]
    public class ServerRaidQueueStatus : IWritable
    {
        /// <summary>Queue position or instance id.</summary>
        public ulong QueueId { get; set; }

        /// <summary>15-bit queued flag.</summary>
        public uint IsQueued { get; set; }

        /// <summary>Wait time or game type value.</summary>
        public ulong WaitValue { get; set; }

        /// <summary>Likely MatchingGameTypeId.</summary>
        public uint MatchingGameTypeId { get; set; }

        /// <summary>Unknown trailing field.</summary>
        public uint Unknown { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(QueueId);
            writer.Write(IsQueued, 15u);
            writer.Write(WaitValue);
            writer.Write(MatchingGameTypeId);
            writer.Write(Unknown);
        }
    }
}
