using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    [Message(GameMessageOpcode.ClientCREDDExchangeRequestInfo)]
    public class ClientCREDDExchangeRequestInfo : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // zero byte message
        }
    }
}
