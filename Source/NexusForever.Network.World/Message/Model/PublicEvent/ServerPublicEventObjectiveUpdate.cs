using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    [Message(GameMessageOpcode.ServerPublicEventObjectiveUpdate)]
    public class ServerPublicEventObjectiveUpdate : IWritable
    {
        public PublicEventObjective Objective { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe 14007b490 reads opcode 0x0132 as the full PublicEventObjective payload.
            Objective.Write(writer);
        }
    }
}
