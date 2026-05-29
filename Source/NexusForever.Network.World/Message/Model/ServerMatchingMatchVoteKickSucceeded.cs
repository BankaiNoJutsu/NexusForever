using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Empty notification sent after a vote kick succeeds.
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x061A</c>
    /// directly to <c>ServerEmpty_ReadPayload</c> (<c>14007d8e0</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchVoteKickSucceeded)]
    public class ServerMatchingMatchVoteKickSucceeded : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
            // Zero byte message
        }
    }
}
