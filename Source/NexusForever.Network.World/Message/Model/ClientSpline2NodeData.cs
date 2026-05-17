using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpline2NodeData)]
    public class ClientSpline2NodeData : IReadable
    {
        public class NodeRecord
        {
            public uint[] Values { get; } = new uint[10];

            public void Read(GamePacketReader reader)
            {
                for (int i = 0; i < Values.Length; i++)
                    Values[i] = reader.ReadUInt();
            }
        }

        public uint Spline2Id { get; private set; }
        public ushort NodeId { get; private set; }
        public byte Mode { get; private set; }
        public List<NodeRecord> Nodes { get; } = [];

        public void Read(GamePacketReader reader)
        {
            Spline2Id = reader.ReadUInt();
            NodeId    = reader.ReadUShort(15u);
            Mode      = reader.ReadByte(2u);

            uint count = reader.ReadUInt();
            for (uint i = 0u; i < count; i++)
            {
                var node = new NodeRecord();
                node.Read(reader);
                Nodes.Add(node);
            }
        }
    }
}
