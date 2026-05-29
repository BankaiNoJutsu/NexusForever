using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingMatchPvpStateInitial_ReadPayload</c> (<c>140099190</c>).
    /// Reads one 2-bit <see cref="MatchTeam"/> followed by one nested
    /// <c>MatchingPvpStateInfo_ReadPayload</c> (<c>140099130</c>) block.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchPvpStateInitial)]
    public class ServerMatchingMatchPvpStateInitial : IWritable
    {
        public MatchTeam Team { get; set; }
        public StateInfo State { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Team, 2u);
            State.Write(writer);
        }
    }
}
