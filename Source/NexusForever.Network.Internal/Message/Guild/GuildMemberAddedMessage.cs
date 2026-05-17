using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Guild
{
    public class GuildMemberAddedMessage
    {
        public ulong GuildId { get; set; }
        public GuildType Type { get; set; }
        public Identity Member { get; set; }
        public byte Rank { get; set; }
        public GuildRankPermission Permissions { get; set; }
    }
}
