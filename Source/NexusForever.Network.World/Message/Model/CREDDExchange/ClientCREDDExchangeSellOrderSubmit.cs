using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    [Message(GameMessageOpcode.ClientCREDDExchangeSellOrderSubmit)]
    public class ClientCREDDExchangeSellOrderSubmit : IReadable
    {
        public Identity TargetIdentity { get; } = new();
        public ulong CreditAmount { get; private set; }
        public bool SubmitFlag { get; private set; }

        public void Read(GamePacketReader reader)
        {
            TargetIdentity.Read(reader);
            CreditAmount = reader.ReadULong();
            SubmitFlag   = reader.ReadBit();
        }
    }
}
