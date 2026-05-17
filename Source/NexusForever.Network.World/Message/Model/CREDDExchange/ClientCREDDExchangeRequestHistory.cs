using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    [Message(GameMessageOpcode.ClientCREDDExchangeRequestHistory)]
    public class ClientCREDDExchangeRequestHistory : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // zero byte message
        }
    }
}
