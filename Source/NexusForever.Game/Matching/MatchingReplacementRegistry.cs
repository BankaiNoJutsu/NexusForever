using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Matching
{
    public sealed class MatchingReplacementRegistry : IMatchingReplacementRegistry
    {
        private readonly object syncRoot = new();
        private readonly Dictionary<Guid, ReplacementEntry> byMatch = [];
        private readonly Dictionary<Guid, Guid> queueGroupToMatch = [];

        public void Register(Guid matchGuid, IMatchingQueueGroup queueGroup, Role requestedRoles)
        {
            lock (syncRoot)
            {
                byMatch[matchGuid] = new ReplacementEntry(queueGroup, requestedRoles);
                queueGroupToMatch[queueGroup.Guid] = matchGuid;
            }
        }

        public bool TryGetMatchGuid(Guid queueGroupGuid, out Guid matchGuid)
        {
            lock (syncRoot)
                return queueGroupToMatch.TryGetValue(queueGroupGuid, out matchGuid);
        }

        public bool TryGetEntry(Guid matchGuid, out IMatchingQueueGroup queueGroup, out Role requestedRoles)
        {
            lock (syncRoot)
            {
                if (byMatch.TryGetValue(matchGuid, out ReplacementEntry entry))
                {
                    queueGroup      = entry.QueueGroup;
                    requestedRoles  = entry.RequestedRoles;
                    return true;
                }
            }

            queueGroup     = null;
            requestedRoles = Role.None;
            return false;
        }

        public void UnregisterByMatch(Guid matchGuid)
        {
            lock (syncRoot)
            {
                if (!byMatch.Remove(matchGuid, out ReplacementEntry entry))
                    return;

                queueGroupToMatch.Remove(entry.QueueGroup.Guid);
            }
        }

        public void UnregisterByQueueGroup(Guid queueGroupGuid)
        {
            lock (syncRoot)
            {
                if (!queueGroupToMatch.Remove(queueGroupGuid, out Guid matchGuid))
                    return;

                byMatch.Remove(matchGuid);
            }
        }

        private sealed record ReplacementEntry(IMatchingQueueGroup QueueGroup, Role RequestedRoles);
    }
}
