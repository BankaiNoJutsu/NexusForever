using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.State)]
    public class State : IReadable, IWritable
    {
        public void Read(GamePacketReader reader)
        {
        }

        public void Write(GamePacketWriter writer)
        {
        }
    }
}
