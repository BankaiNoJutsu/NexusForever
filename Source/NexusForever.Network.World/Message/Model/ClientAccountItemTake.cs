using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientAccountItemTake)]
    public class ClientAccountItemTake : IReadable
    {
        public ulong Id { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Id = reader.ReadULong();
        }
    }
}
