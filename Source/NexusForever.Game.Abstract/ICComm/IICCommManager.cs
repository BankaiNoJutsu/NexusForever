using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.ICComm;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.ICComm
{
    public interface IICCommManager : IUpdate
    {
        ICCommJoinResult? Join(IPlayer player, ICCommChannelType type, ulong guildId, string name, out ulong iccommId, out string channelName);
        ICCommMessageResult SendMessage(IPlayer player, ulong iccommId, uint messageId, string message, string recipientName);
        bool RemoveClientMembership(IPlayer player, ulong iccommId);
    }
}
