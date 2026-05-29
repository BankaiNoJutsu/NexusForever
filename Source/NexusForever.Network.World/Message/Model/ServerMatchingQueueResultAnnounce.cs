using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingQueueResultAnnounce_ReadPayload</c> (<c>1400998c0</c>).
    /// Reads one 6-bit <see cref="MatchingQueueResult"/> followed by one 4-bit
    /// <see cref="MatchingQueueStatus"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingQueueResultAnnounce)]
    public class ServerMatchingQueueResultAnnounce : IWritable
    {
        public MatchingQueueResult Result { get; set; }
        public MatchingQueueStatus Status { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Result, 6u);
            writer.Write(Status, 4u);
        }
    }
}
