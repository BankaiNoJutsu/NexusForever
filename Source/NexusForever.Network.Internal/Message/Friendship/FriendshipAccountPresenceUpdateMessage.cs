using NexusForever.Game.Static.Chat;

namespace NexusForever.Network.Internal.Message.Friendship
{
    public class FriendshipAccountPresenceUpdateMessage
    {
        public uint AccountId { get; set; }
        public Shared.FriendshipAccountCommandOwner PresenceOwner { get; set; }
        public AccountPresenceState Presence { get; set; }
    }
}
