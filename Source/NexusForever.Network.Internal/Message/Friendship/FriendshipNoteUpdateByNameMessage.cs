using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Friendship
{
    public class FriendshipNoteUpdateByNameMessage
    {
        public Identity Source { get; set; }
        public IdentityName TargetName { get; set; }
        public string Note { get; set; }
    }
}
