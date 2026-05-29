using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingPvpRatingUpdated_ReadPayload</c> (<c>140099330</c>).
    /// Reads four uint32 rating counters followed by one 3-bit
    /// <see cref="MatchingGameRatingType"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingPvpRatingUpdated)]
    public class ServerMatchingPvpRatingUpdated : IWritable
    {
        public uint Rating { get; set; }
        public uint Wins { get; set; }
        public uint Losses { get; set; }
        public uint Draws { get; set; }
        public MatchingGameRatingType Type { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Rating);
            writer.Write(Wins);
            writer.Write(Losses);
            writer.Write(Draws);
            writer.Write(Type, 3u);
        }
    }
}
