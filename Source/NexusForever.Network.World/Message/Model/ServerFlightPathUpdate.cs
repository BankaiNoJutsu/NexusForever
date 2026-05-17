using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerFlightPathUpdate)]
    public class ServerFlightPathUpdate : IWritable
    {
        public List<uint> FlightPathIds { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)FlightPathIds.Count);
            FlightPathIds.ForEach(id => writer.Write(id));
        }
    }
}
