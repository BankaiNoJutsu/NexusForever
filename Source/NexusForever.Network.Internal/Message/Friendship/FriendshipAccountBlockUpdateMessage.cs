using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Friendship
{
    public class FriendshipAccountBlockUpdateMessage
    {
        public Identity Identity { get; set; }
        public uint AccountId { get; set; }
        public bool BlockFriendRequests { get; set; }
    }
}
