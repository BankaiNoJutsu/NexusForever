using NexusForever.Game.Static.Guild;

namespace NexusForever.Network.Internal.Message.Guild
{
    public class GuildCreatedMessage
    {
        public ulong GuildId { get; set; }
        public GuildType Type { get; set; }
    }
}
