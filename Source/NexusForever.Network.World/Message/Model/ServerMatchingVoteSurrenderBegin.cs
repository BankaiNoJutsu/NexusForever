using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Runtime uses this empty packet to begin surrender votes in PvP matches and disband votes in PvE matches.
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x0621</c>
    /// directly to <c>ServerEmpty_ReadPayload</c> (<c>14007d8e0</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchVoteSurrenderBegin)]
    public class ServerMatchingMatchVoteSurrenderBegin : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
            // Zero byte message
        }
    }
}
