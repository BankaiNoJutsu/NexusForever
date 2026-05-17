using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerUiWindowState)]
    public class ServerUiWindowState : IWritable
    {
        public uint WindowId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(WindowId);
        }
    }
}
