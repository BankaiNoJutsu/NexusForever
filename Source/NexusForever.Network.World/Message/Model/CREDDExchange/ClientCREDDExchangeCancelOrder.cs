using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    [Message(GameMessageOpcode.ClientCREDDExchangeCancelOrder)]
    public class ClientCREDDExchangeCancelOrder : IReadable
    {
        public ulong OrderId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            OrderId = reader.ReadULong();
        }
    }
}
