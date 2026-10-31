using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientVehicleEmbark)]
    public class ClientVehicleEmbark : IReadable
    {
        public uint VehicleUnitId { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }

        public void Read(GamePacketReader reader)
        {
            VehicleUnitId = reader.ReadUInt();
            // Native 0x01B0 writer 140089250 packs two and three-bit contexts.
            // Keep their semantics diagnostic until the sender contract is mapped.
            Unknown1 = reader.ReadUInt(2u);
            Unknown2 = reader.ReadUInt(3u);
        }
    }
}
