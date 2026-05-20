using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientStorefrontPurchaseAccount)]
    public class ClientStorefrontPurchaseAccount : IReadable
    {
        public uint OfferId { get; private set; }
        public byte Selector { get; private set; }
        public uint Unknown2 { get; private set; }
        public ushort CurrencyId { get; private set; }
        public uint Unknown4 { get; private set; }
        public Identity Target { get; } = new();
        public uint Unknown6 { get; private set; }
        public uint Unknown7 { get; private set; }
        public Identity AccountTarget { get; } = new();
        public string RecipientName { get; private set; }

        public void Read(GamePacketReader reader)
        {
            OfferId       = reader.ReadUInt();
            Selector      = reader.ReadByte(5u);
            Unknown2      = reader.ReadUInt();
            CurrencyId    = reader.ReadUShort(14u);
            Unknown4      = reader.ReadUInt();
            Target.Read(reader);
            Unknown6      = reader.ReadUInt();
            Unknown7      = reader.ReadUInt();
            AccountTarget.Read(reader);
            RecipientName = reader.ReadWideString();
        }
    }
}
