using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x0623</c>
    /// to shared <c>MatchingQueueResultWaitTime_ReadPayload</c> (<c>14007fcf0</c>).
    /// The native reader proves one 6-bit <see cref="MatchingQueueResult"/> followed by one uint32 wait-time field.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchOperationResult)]
    public class ServerMatchingMatchOperationResult : IWritable
    {
        public MatchingQueueResult Result { get; set; }
        public uint WaitTimeBeforeVoteMS { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Result, 6u);
            writer.Write(WaitTimeBeforeVoteMS);
        }
    }
}
