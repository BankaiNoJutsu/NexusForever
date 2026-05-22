using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Raid queue status update (0x0718, 0x20 bytes).
    /// Neighbor of <see cref="Pregame.ServerQueueStatus"/> (0x0717).
    /// </summary>
    [Message(GameMessageOpcode.ServerRaidQueueStatus)]
    public class ServerRaidQueueStatus : IWritable
    {
        public uint QueuePosition { get; set; }

        public uint WaitTimeSeconds { get; set; }

        public bool IsQueued { get; set; }

        public uint MatchingGameTypeId { get; set; }

        public uint Unknown0 { get; set; }

        public uint Unknown1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(QueuePosition);
            writer.Write(WaitTimeSeconds);
            writer.Write(IsQueued);
            writer.Write(MatchingGameTypeId);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            writer.WriteBytes(new byte[11]);
        }
    }
}
