using NexusForever.Network.Internal.Message.Shared;
using InternalGroup = NexusForever.Network.Internal.Message.Group.Shared.Group;

namespace NexusForever.Network.Internal.Message.Group
{
    public class GroupInstanceDifficultyUpdatedMessage
    {
        public InternalGroup Group { get; set; }
        public Identity Setter { get; set; }
    }
}
