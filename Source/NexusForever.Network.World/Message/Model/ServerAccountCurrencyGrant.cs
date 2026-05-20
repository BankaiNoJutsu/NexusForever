using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountCurrencyGrant)]
    public class ServerAccountCurrencyGrant : IWritable
    {
        public AccountCurrency AccountCurrency { get; set; }
        public ulong Reason { get; set; }
        public ulong Reserved { get; set; }

        public void Write(GamePacketWriter writer)
        {
            AccountCurrency.Write(writer);
            writer.Write(Reason);
            writer.Write(Reserved);
        }
    }
}
