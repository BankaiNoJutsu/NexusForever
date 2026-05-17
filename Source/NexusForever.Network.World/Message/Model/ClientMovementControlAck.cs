using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMovementControlAck)]
    public class ClientMovementControlAck : IReadable
    {
        public uint Ticket { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Ticket = reader.ReadUInt();
        }
    }
}
