namespace NexusForever.Network.Internal.Message.Guild
{
    public class GuildDisbandedMessage
    {
        public ulong GuildId { get; set; }
        public NexusForever.Game.Static.Guild.GuildType Type { get; set; }
    }
}
