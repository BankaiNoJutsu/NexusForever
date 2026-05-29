using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Story
{
    [Message(GameMessageOpcode.ServerCommunicatorMessage)]
    public class ServerCommunicatorMessage : IWritable
    {
        public ushort CommunicatorMessagesId { get; set; }

        // If false, this will make the message appear even if the player doesn't meet the
        // conditions for the message
        public bool CheckConditions { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe exports DB\CommunicatorMessages.tbl and
            // Communicator_ShowQuestMsg anchors; send timing/conditions stay LWS-075 gated.
            writer.Write(CommunicatorMessagesId, 15u);
            writer.Write(CheckConditions);
        }
    }
}
