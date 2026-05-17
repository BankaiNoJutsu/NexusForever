using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingNeighborInviteResponse)]
    public class ClientHousingNeighborInviteResponse : IReadable
    {
        public bool Accepted { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Accepted = reader.ReadBit();
        }
    }
}