using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientAccountItemGiftPendingItemGroupToCharacter)]
    public class ClientAccountItemGiftPendingItemGroupToCharacter : IReadable
    {
        public string Group { get; private set; }
        public Identity TargetCharacter { get; } = new();

        public void Read(GamePacketReader reader)
        {
            Group = reader.ReadWideString();
            TargetCharacter.Read(reader);
        }
    }
}
