using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMatchingGameReadyResponse)]
    public class ClientMatchingGameReadyResponse : IReadable
    {
        public bool Response { get; private set; } // decline = false, join = true

        public void Read(GamePacketReader reader)
        {
            Response = reader.ReadBit();
        }
    }
}
