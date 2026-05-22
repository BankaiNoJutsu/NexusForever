using System.Collections.Concurrent;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Group;
using NexusForever.Shared;

namespace NexusForever.Game.Group
{
    public class GroupStateManager : Singleton<GroupStateManager>, IGroupStateManager
    {
        private readonly ConcurrentDictionary<ulong, GroupLootState> groups = [];
        private readonly ConcurrentDictionary<Identity, ulong> characterGroups = [];
        private readonly ConcurrentDictionary<ulong, uint> roundRobinLastGroupIndices = [];
        private readonly object roundRobinLock = new();

        public void Update(double lastTick)
        {
        }

        public void UpdateGroup(GroupLootState group)
        {
            if (group == null || group.GroupId == 0ul)
                return;

            if (groups.TryGetValue(group.GroupId, out GroupLootState existing))
            {
                foreach (GroupLootMember member in existing.Members)
                    characterGroups.TryRemove(member.Identity, out _);
            }

            GroupLootState orderedGroup = new()
            {
                GroupId          = group.GroupId,
                NormalRule       = group.NormalRule,
                ThresholdRule    = group.ThresholdRule,
                ThresholdQuality = group.ThresholdQuality,
                HarvestRule      = group.HarvestRule,
                Leader           = group.Leader,
                Members          = group.Members
                    .OrderBy(m => m.GroupIndex)
                    .ThenBy(m => m.Identity.Id)
                    .ToList()
            };

            groups[group.GroupId] = orderedGroup;

            foreach (GroupLootMember member in orderedGroup.Members)
                characterGroups[member.Identity] = orderedGroup.GroupId;
        }

        public void RemoveGroup(ulong groupId)
        {
            if (!groups.TryRemove(groupId, out GroupLootState group))
                return;

            foreach (GroupLootMember member in group.Members)
                characterGroups.TryRemove(member.Identity, out _);

            roundRobinLastGroupIndices.TryRemove(groupId, out _);
        }

        public void RemoveMember(ulong groupId, Identity identity)
        {
            characterGroups.TryRemove(identity, out _);

            if (!groups.TryGetValue(groupId, out GroupLootState group))
                return;

            GroupLootState updatedGroup = group.WithoutMember(identity);
            if (updatedGroup.Members.Count == 0)
            {
                RemoveGroup(groupId);
                return;
            }

            groups[groupId] = updatedGroup;
        }

        public void RemoveCharacter(Identity identity)
        {
            if (!characterGroups.TryRemove(identity, out ulong groupId))
                return;

            RemoveMember(groupId, identity);
        }

        public bool TryGetGroup(ulong groupId, out GroupLootState group)
        {
            return groups.TryGetValue(groupId, out group);
        }

        public bool TryGetGroupForCharacter(Identity identity, out GroupLootState group)
        {
            group = null;

            if (!characterGroups.TryGetValue(identity, out ulong groupId))
                return false;

            return TryGetGroup(groupId, out group);
        }

        public GroupLootMember NextRoundRobinWinner(GroupLootState group, IReadOnlyList<GroupLootMember> eligibleMembers)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (eligibleMembers == null || eligibleMembers.Count == 0)
                throw new ArgumentException("At least one eligible member is required.", nameof(eligibleMembers));

            HashSet<ulong> eligibleCharacterIds = eligibleMembers
                .Select(m => m.Identity.Id)
                .ToHashSet();

            List<GroupLootMember> orderedMembers = group.Members
                .Where(m => eligibleCharacterIds.Contains(m.Identity.Id))
                .OrderBy(m => m.GroupIndex)
                .ThenBy(m => m.Identity.Id)
                .ToList();

            if (orderedMembers.Count == 0)
            {
                orderedMembers = eligibleMembers
                    .OrderBy(m => m.GroupIndex)
                    .ThenBy(m => m.Identity.Id)
                    .ToList();
            }

            lock (roundRobinLock)
            {
                GroupLootMember winner = orderedMembers[0];
                if (roundRobinLastGroupIndices.TryGetValue(group.GroupId, out uint lastGroupIndex))
                {
                    winner = orderedMembers.FirstOrDefault(m => m.GroupIndex > lastGroupIndex)
                        ?? orderedMembers[0];
                }

                roundRobinLastGroupIndices[group.GroupId] = winner.GroupIndex;
                return winner;
            }
        }
    }
}
