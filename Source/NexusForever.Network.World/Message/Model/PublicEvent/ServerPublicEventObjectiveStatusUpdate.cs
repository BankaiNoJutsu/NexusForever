using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    [Message(GameMessageOpcode.ServerPublicEventObjectiveStatusUpdate)]
    public class ServerPublicEventObjectiveStatusUpdate : IWritable
    {
        public uint ObjectiveId { get; set; }
        public PublicEventObjectiveStatus ObjectiveStatus { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe 14007b3c0 reads opcode 0x0134 as objectiveId u15 + PublicEventObjectiveStatus.
            writer.Write(ObjectiveId, 15);
            ObjectiveStatus.Write(writer);
        }
    }
}
