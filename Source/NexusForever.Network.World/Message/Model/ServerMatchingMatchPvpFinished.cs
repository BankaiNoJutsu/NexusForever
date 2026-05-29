using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingMatchPvpFinished_ReadPayload</c> (<c>140099200</c>).
    /// Reads one 2-bit <see cref="MatchWinner"/>, one 3-bit <see cref="MatchEndReason"/>,
    /// and two trailing uint32 rating-change fields.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchPvpFinished)]
    public class ServerMatchingMatchPvpFinished : IWritable
    {
        public MatchWinner Winner { get; set; }
        public MatchEndReason Reason { get; set; }
        public uint RatingChangeTeam1 { get; set; }
        public uint RatingChangeTeam2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Winner, 2u);
            writer.Write(Reason, 3u);
            writer.Write(RatingChangeTeam1);
            writer.Write(RatingChangeTeam2);
        }
    }
}
