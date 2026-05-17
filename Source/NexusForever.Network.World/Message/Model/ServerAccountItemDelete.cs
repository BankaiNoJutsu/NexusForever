using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountItemDelete)]
    public class ServerAccountItemDelete : IWritable
    {
        public ulong Id { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Id);
        }
    }
}
