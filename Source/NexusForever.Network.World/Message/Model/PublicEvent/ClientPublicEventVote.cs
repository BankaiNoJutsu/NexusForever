using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    [Message(GameMessageOpcode.ClientPublicEventVote)]
    public class ClientPublicEventVote : IReadable
    {
        public uint EventId { get; private set; }
        public uint VoteId { get; private set; }
        public uint TeamId { get; private set; }
        public uint Choice { get; private set; }

        public void Read(GamePacketReader reader)
        {
            // WildStar64.exe registers client opcode 0x06EE with a 0x10-byte payload:
            // event id u14, vote id u14, team id u14, and choice u32.
            EventId = reader.ReadUInt(14);
            VoteId = reader.ReadUInt(14);
            TeamId = reader.ReadUInt(14);
            Choice = reader.ReadUInt();
        }
    }
}
