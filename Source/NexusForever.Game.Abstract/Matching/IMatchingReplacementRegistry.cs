using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Abstract.Matching
{
    public interface IMatchingReplacementRegistry
    {
        void Register(Guid matchGuid, IMatchingQueueGroup queueGroup, Role requestedRoles);

        bool TryGetMatchGuid(Guid queueGroupGuid, out Guid matchGuid);

        bool TryGetEntry(Guid matchGuid, out IMatchingQueueGroup queueGroup, out Role requestedRoles);

        void UnregisterByMatch(Guid matchGuid);

        void UnregisterByQueueGroup(Guid queueGroupGuid);
    }
}
