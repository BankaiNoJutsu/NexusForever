using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.CREDDExchange
{
    /// <summary>Build 16042 account-inventory CREDD redeem request with an empty payload.</summary>
    [Message(GameMessageOpcode.ClientAccountCREDDRedeem)]
    public sealed class ClientAccountCREDDRedeem : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }
}
