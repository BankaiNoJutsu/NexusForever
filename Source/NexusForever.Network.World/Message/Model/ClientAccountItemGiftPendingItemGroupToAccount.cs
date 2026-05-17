using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientAccountItemGiftPendingItemGroupToAccount)]
    public class ClientAccountItemGiftPendingItemGroupToAccount : IReadable
    {
        public string Group { get; private set; }
        public ulong TargetAccountId { get; private set; }
        public uint Unknown0 { get; private set; }
        public Identity SenderCharacter { get; } = new();

        public void Read(GamePacketReader reader)
        {
            Group           = reader.ReadWideString();
            TargetAccountId = reader.ReadULong();
            Unknown0        = reader.ReadUInt();
            SenderCharacter.Read(reader);
        }
    }
}
