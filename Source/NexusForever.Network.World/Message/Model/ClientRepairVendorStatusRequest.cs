using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRepairVendorStatusRequest)]
    public class ClientRepairVendorStatusRequest : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }
}