using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientStorefrontPurchaseCharacter)]
    public class ClientStorefrontPurchaseCharacter : IReadable
    {
        public uint OfferId { get; private set; }
        public byte Selector { get; private set; }
        public uint Unknown2 { get; private set; }
        public ushort CurrencyId { get; private set; }
        public uint Unknown4 { get; private set; }
        public Identity Target { get; } = new();
        public uint Unknown6 { get; private set; }

        public void Read(GamePacketReader reader)
        {
            OfferId    = reader.ReadUInt();
            Selector   = reader.ReadByte(5);
            Unknown2   = reader.ReadUInt();
            CurrencyId = reader.ReadUShort(14);
            Unknown4   = reader.ReadUInt();
            Target.Read(reader);
            Unknown6   = reader.ReadUInt();
        }
    }
}
