using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Friendship.Shared
{
    public class FriendshipAccountCommandOwner
    {
        public Identity Identity { get; set; }
        public ulong SessionEpoch { get; set; }
        public ulong Admission { get; set; }
    }
}
