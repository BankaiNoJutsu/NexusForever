using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    /// <summary>
    /// Legacy emulator CREDD-to-Credits request (opcode 0x0268).
    /// The build 16042 account-inventory action uses <see cref="ClientAccountCREDDRedeem"/>.
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
