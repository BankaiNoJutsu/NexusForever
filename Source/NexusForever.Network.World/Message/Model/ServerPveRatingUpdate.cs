using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Mirrors ServerPvpRatingUpdate's four trailing counters on the wire.
    // This triggers the lua event PveRatingUpdated but there are no uses of it in Carbine's lua code.
    [Message(GameMessageOpcode.ServerPveRatingUpdate)]
    public class ServerPveRatingUpdate : IWritable
    {
        public class PveRating : IWritable
        {
            public uint Category { get; set; } // similar to ServerPvpRatingUpdate, not sure what the categories are
            public MatchingGameRatingType Type { get; set; }
            public uint Rating { get; set; }
            public uint Wins { get; set; }
            public uint Losses { get; set; }
            public uint Draws { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Category, 8u);
                writer.Write(Type, 3u);
                writer.Write(Rating);
                writer.Write(Wins);
                writer.Write(Losses);
                writer.Write(Draws);
            }
        }

        public List<PveRating> PveRatings { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PveRatings.Count);
            foreach (var rating in PveRatings)
            {
                rating.Write(writer);
            }
        }
    }
}
