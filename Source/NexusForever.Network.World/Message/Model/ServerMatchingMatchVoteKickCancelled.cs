using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Triggers the <c>MatchVoteKickEnd</c> Lua event on the client.
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x060D</c>
    /// directly to <c>ServerEmpty_ReadPayload</c> (<c>14007d8e0</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchVoteKickCancelled)]
    public class ServerMatchingMatchVoteKickCancelled : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
            // Zero byte message
        }
    }
}
