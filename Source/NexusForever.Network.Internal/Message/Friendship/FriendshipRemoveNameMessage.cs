using NexusForever.Game.Static.Friendship;
using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Friendship
{
    public class FriendshipRemoveNameMessage
    {
        public Identity Inviter { get; set; }
        public IdentityName InviteeName { get; set; }
        public FriendshipType Type { get; set; }
    }
}
