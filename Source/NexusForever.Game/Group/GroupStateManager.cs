using System.Collections.Concurrent;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Static.Group;

namespace NexusForever.Game.Group
{
    public class GroupStateManager : IGroupStateManager
    {
        private readonly ConcurrentDictionary<ulong, GroupLootState> groups = [];
        private readonly ConcurrentDictionary<Identity, ulong> characterGroups = [];
        private readonly ConcurrentDictionary<ulong, uint> roundRobinLastGroupIndices = [];
        private readonly object roundRobinLock = new();
        private readonly object stateLock = new();
        private readonly HashSet<Identity> explicitAssociations = [];

        public void Update(double lastTick)
        {
        }

        public void UpdateGroup(GroupLootState group)
        {
            lock (stateLock)
                UpdateGroupCore(group);
        }

        public void UpdateGroupAssociation(GroupLootState group, Identity identity)
        {
            lock (stateLock)
            {
                UpdateGroupCore(group);
                explicitAssociations.Add(identity);
                characterGroups[identity] = group?.GroupId ?? 0ul;
            }
        }

        private void UpdateGroupCore(GroupLootState group)
        {
            if (group == null || group.GroupId == 0ul)
                return;

            if (groups.TryGetValue(group.GroupId, out GroupLootState existing))
            {
                foreach (GroupLootMember member in existing.Members)
                    if (!group.HasMember(member.Identity))
                        RemoveAssociationForGroup(member.Identity, group.GroupId);
            }

            GroupLootState orderedGroup = new()
            {
                GroupId          = group.GroupId,
                NormalRule       = group.NormalRule,
                ThresholdRule    = group.ThresholdRule,
                ThresholdQuality = group.ThresholdQuality,
                HarvestRule      = group.HarvestRule,
                InstanceDifficulty = group.InstanceDifficulty,
                Flags            = group.Flags,
                Leader           = group.Leader,
                Members          = group.Members
                    .OrderBy(m => m.GroupIndex)
                    .ThenBy(m => m.Identity.Id)
                    .ToList().AsReadOnly()
            };

            groups[group.GroupId] = orderedGroup;

            foreach (GroupLootMember member in orderedGroup.Members)
                if (!explicitAssociations.Contains(member.Identity))
                    characterGroups.TryAdd(member.Identity, orderedGroup.GroupId);
        }

        public void RemoveGroup(ulong groupId)
        {
            lock (stateLock)
                RemoveGroupCore(groupId);
        }

        private void RemoveGroupCore(ulong groupId)
        {
            if (!groups.TryRemove(groupId, out GroupLootState group))
                return;

            foreach (GroupLootMember member in group.Members)
                RemoveAssociationForGroup(member.Identity, groupId);

            roundRobinLastGroupIndices.TryRemove(groupId, out _);
        }

        public void RemoveMember(ulong groupId, Identity identity)
        {
            lock (stateLock)
                RemoveMemberCore(groupId, identity);
        }

        private void RemoveAssociationForGroup(Identity identity, ulong groupId)
        {
            if (!characterGroups.TryGetValue(identity, out ulong associated) || associated != groupId)
                return;

            if (explicitAssociations.Contains(identity))
                characterGroups[identity] = 0ul;
            else
                characterGroups.TryRemove(identity, out _);
        }

        private void RemoveMemberCore(ulong groupId, Identity identity)
        {
            RemoveAssociationForGroup(identity, groupId);

            if (!groups.TryGetValue(groupId, out GroupLootState group))
                return;

            GroupLootState updatedGroup = group.WithoutMember(identity);
            if (updatedGroup.Members.Count == 0)
            {
                RemoveGroupCore(groupId);
                return;
            }

            groups[groupId] = updatedGroup;
        }

        public void RemoveCharacter(Identity identity)
        {
            // This is an authoritative association removal, not a membership edit.
            // Later secondary-group snapshots must not silently restore a primary.
            lock (stateLock)
            {
                explicitAssociations.Add(identity);
                characterGroups[identity] = 0ul;
            }
        }

        public bool TryGetGroup(ulong groupId, out GroupLootState group)
        {
            lock (stateLock)
                return groups.TryGetValue(groupId, out group);
        }

        public bool TryGetGroupForCharacter(Identity identity, out GroupLootState group)
        {
            lock (stateLock)
            {
                group = null;
                if (!characterGroups.TryGetValue(identity, out ulong groupId)
                    || !groups.TryGetValue(groupId, out GroupLootState candidate)
                    || !candidate.HasMember(identity))
                    return false;

                group = candidate;
                return true;
            }
        }

        public GroupLootMember NextRoundRobinWinner(GroupLootState group, IReadOnlyList<GroupLootMember> eligibleMembers)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (eligibleMembers == null || eligibleMembers.Count == 0)
                throw new ArgumentException("At least one eligible member is required.", nameof(eligibleMembers));

            HashSet<Identity> eligibleCharacterIds = eligibleMembers
                .Select(m => m.Identity)
                .ToHashSet();

            List<GroupLootMember> orderedMembers = group.Members
                .Where(m => eligibleCharacterIds.Contains(m.Identity))
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

        public GroupLootMember ResolveHarvestLootRecipient(GroupLootState group, IPlayer harvester, IReadOnlyList<GroupLootMember> eligibleMembers)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (harvester == null)
                throw new ArgumentNullException(nameof(harvester));

            if (eligibleMembers == null || eligibleMembers.Count == 0)
                throw new ArgumentException("At least one eligible member is required.", nameof(eligibleMembers));

            if (group.HarvestRule == HarvestLootRule.FirstTagger)
            {
                GroupLootMember tagger = eligibleMembers.FirstOrDefault(m => m.Identity.Id == harvester.CharacterId);
                return tagger ?? eligibleMembers[0];
            }

            return NextRoundRobinWinner(group, eligibleMembers);
        }
    }
}
