using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // WildStar64.exe registers opcode 0x06F9 with the shared 15-bit scalar reader at 14007c3a0.
    [Message(GameMessageOpcode.ServerPublicEventObjectiveStart)]
    public class ServerPublicEventObjectiveStart : IWritable
    {
        public uint ObjectiveId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ObjectiveId, 15);
        }
    }
}
