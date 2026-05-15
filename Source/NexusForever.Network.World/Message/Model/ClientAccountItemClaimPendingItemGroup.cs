using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientAccountItemClaimPendingItemGroup)]
    public class ClientAccountItemClaimPendingItemGroup : IReadable
    {
        public string Group { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Group = reader.ReadWideString();
        }
    }
}
