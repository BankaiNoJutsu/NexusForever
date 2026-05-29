using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    [Message(GameMessageOpcode.ServerPublicEventVoteEnd)]
    public class ServerPublicEventVoteEnd : IWritable
    {
        public uint EventId { get; set; }
        public uint VoteId { get; set; }
        public uint Winner { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe registration maps opcode 0x06FD to the shared vote result row:
            // event id u14, vote id u14, and winner u32.
            writer.Write(EventId, 14);
            writer.Write(VoteId, 14);
            writer.Write(Winner);
        }
    }
}
