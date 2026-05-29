using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Triggers the <c>MatchVoteKickBegin</c> Lua event on the client.
    /// Native reader: <c>ServerMatchingMatchVoteKickBegin_ReadPayload</c> (<c>140099840</c>).
    /// Reads one initiator <see cref="Identity"/> followed by one member-to-kick <see cref="Identity"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchVoteKickBegin)]
    public class ServerMatchingMatchVoteKickBegin : IWritable
    {
        public Identity Initiator { get; set; } = new();
        public Identity MemberToKick { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            Initiator.Write(writer);
            MemberToKick.Write(writer);
        }
    }
}
