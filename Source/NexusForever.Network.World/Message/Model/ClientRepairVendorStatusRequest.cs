using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Native SellJunkToVendor sends this empty payload on opcode 0x0167.
    [Message(GameMessageOpcode.ClientRepairVendorStatusRequest)]
    public class ClientRepairVendorStatusRequest : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }
}
