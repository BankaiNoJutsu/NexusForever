using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    [Message(GameMessageOpcode.ClientCREDDExchangeBuyOrderSubmit)]
    public class ClientCREDDExchangeBuyOrderSubmit : IReadable
    {
        public ulong CreditAmount { get; private set; }
        public bool SubmitFlag { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CreditAmount = reader.ReadULong();
            SubmitFlag   = reader.ReadBit();
        }
    }
}
