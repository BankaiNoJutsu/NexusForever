using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpline2NodePositionData)]
    public class ClientSpline2NodePositionData : IReadable
    {
        public uint Spline2Id { get; private set; }
        public uint NodeId { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public List<uint> Values { get; } = [];

        public void Read(GamePacketReader reader)
        {
            Spline2Id = reader.ReadUInt();
            NodeId    = reader.ReadUInt();
            X         = reader.ReadSingle();
            Y         = reader.ReadSingle();
            Z         = reader.ReadSingle();

            uint count = reader.ReadUInt();
            for (uint i = 0u; i < count; i++)
                Values.Add(reader.ReadUInt());
        }
    }
}
