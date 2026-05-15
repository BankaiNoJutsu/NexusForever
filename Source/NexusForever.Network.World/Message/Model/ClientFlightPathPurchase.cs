using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientFlightPathPurchase)]
    public class ClientFlightPathPurchase : IReadable
    {
        public List<uint> RouteIds { get; } = [];

        public void Read(GamePacketReader reader)
        {
            uint routeCount = reader.ReadUInt();
            if (routeCount > reader.BytesRemaining / 4u)
                throw new InvalidPacketValueException();

            for (uint i = 0u; i < routeCount; i++)
                RouteIds.Add(reader.ReadUInt());
        }
    }
}
