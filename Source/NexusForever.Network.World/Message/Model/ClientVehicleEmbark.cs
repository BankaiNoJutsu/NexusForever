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
            Unknown1 = reader.ReadUInt();
            Unknown2 = reader.ReadUInt();
        }
    }
}