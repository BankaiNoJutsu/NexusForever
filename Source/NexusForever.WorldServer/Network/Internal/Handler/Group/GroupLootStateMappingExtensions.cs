using System.Linq;
using NexusForever.Game;
using NexusForever.Game.Abstract.Group;
using InternalGroup = NexusForever.Network.Internal.Message.Group.Shared.Group;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public static class GroupLootStateMappingExtensions
    {
        public static GroupLootState ToGroupLootState(this InternalGroup group)
        {
            return new GroupLootState
            {
                GroupId          = group.Id,
                NormalRule       = group.NormalRule,
                ThresholdRule    = group.ThresholdRule,
                ThresholdQuality = group.ThresholdQuality,
                HarvestRule      = group.HarvestRule,
                Flags            = group.Flags,
                Leader           = group.Leader.ToGameIdentity(),
                Members          = group.Members
                    .Select(m => new GroupLootMember
                    {
                        Identity   = m.Identity.ToGameIdentity(),
                        GroupIndex = m.GroupIndex
                    })
                    .ToList()
            };
        }
    }
}
