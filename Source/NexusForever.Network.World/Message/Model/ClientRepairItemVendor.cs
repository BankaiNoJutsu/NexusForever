using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRepairItemVendor)]
    public class ClientRepairItemVendor : IReadable
    {
        public ulong ItemIdentity { get; private set; }
        public ulong OptionalField { get; private set; }
        public ulong RepairCost { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ItemIdentity = reader.ReadULong();
            OptionalField = reader.ReadULong();
            RepairCost = reader.ReadULong();
        }
    }
}