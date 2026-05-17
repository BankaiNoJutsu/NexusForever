using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.State2)]
    public class State2 : IReadable, IWritable
    {
        public void Read(GamePacketReader reader)
        {
        }

        public void Write(GamePacketWriter writer)
        {
        }
    }
}
