using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    /// <summary>
    /// Redeem account CREDD currency into character credits (opcode 0x0268).
    /// </summary>
    [Message(GameMessageOpcode.ClientCREDDRedeem)]
    public class ClientCREDDRedeem : IReadable
    {
        public ulong CreddAmount { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CreddAmount = reader.ReadULong();
        }
    }
}
