using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // Player automatically has the right to vote on the event when started this way.
    [Message(GameMessageOpcode.ServerPublicEventVoteInitiate)]
    public class ServerPublicEventVoteInitiate : IWritable
    {
        public uint EventId { get; set; }
        public uint VoteId { get; set; }
        public uint TeamId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe registration maps opcode 0x06F1 to a 0x0c-byte reader:
            // event id u14, vote id u14, and team id u14.
            writer.Write(EventId, 14);
            writer.Write(VoteId, 14);
            writer.Write(TeamId, 14);
        }
    }
}
