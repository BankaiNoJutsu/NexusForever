using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerHousingNeighbors)]
    public class ServerHousingNeighbors : IWritable
    {
        public class Neighbor : IWritable
        {
            public ulong CharacterId { get; set; }
            public ulong Reserved { get; set; }
            public TargetResidence TargetResidence { get; } = new();
            public uint Permission { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(CharacterId);
                writer.Write(Reserved);
                TargetResidence.Write(writer);
                writer.Write(Permission);
            }
        }

        public List<Neighbor> Neighbors { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Neighbors.Count);
            Neighbors.ForEach(neighbor => neighbor.Write(writer));
        }
    }
}
