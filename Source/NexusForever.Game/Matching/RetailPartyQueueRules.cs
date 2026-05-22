using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Group;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Shared;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching
{
    /// <summary>
    /// Retail group-finder party queue rules (build 16042): leader-only group join and post-dungeon requeue.
    /// </summary>
    public static class RetailPartyQueueRules
    {
        public static MatchingQueueResult? ValidateGroupLeader(IPlayer player, IGroupStateManager groupStateManager)
        {
            if (!groupStateManager.TryGetGroupForCharacter(player.Identity, out GroupLootState group))
                return null;

            return player.Identity.Equals(group.Leader)
                ? null
                : MatchingQueueResult.GroupMemberPrivilegeRestricted;
        }

        public static List<Identity> ResolvePartyMemberIdentities(
            IPlayer player,
            IGroupStateManager groupStateManager,
            IEnumerable<IPlayer> nearbyPlayers)
        {
            if (groupStateManager.TryGetGroupForCharacter(player.Identity, out GroupLootState group))
            {
                List<Identity> identities = group.Members
                    .Select(m => m.Identity)
                    .ToList();

                if (!identities.Any(i => i.Equals(player.Identity)))
                    identities.Insert(0, player.Identity);

                return identities;
            }

            List<Identity> nearby = nearbyPlayers
                .Select(p => p.Identity)
                .ToList();

            if (!nearby.Any(i => i.Equals(player.Identity)))
                nearby.Insert(0, player.Identity);

            return nearby;
        }

        public static bool TryCreateFinishedGroupRequeueProposal(
            IPlayer leader,
            Role leaderRoles,
            MatchType matchType,
            IEnumerable<IMatchingMap> matchingMaps,
            MatchingQueueFlags matchingQueueFlags,
            IGroupStateManager groupStateManager,
            IPlayerManager playerManager,
            IMatchManager matchManager,
            IMatchingDataManager matchingDataManager,
            IFactory<IMatchingQueueProposal> matchingQueueProposalFactory,
            out IMatchingQueueProposal matchingQueueProposal)
        {
            matchingQueueProposal = null;

            if (!matchingDataManager.CanRequeueAsGroup(matchType))
                return false;

            if (!groupStateManager.TryGetGroupForCharacter(leader.Identity, out GroupLootState group))
                return false;

            if (!leader.Identity.Equals(group.Leader))
                return false;

            List<(IPlayer Player, Role Roles)> members = [];
            foreach (GroupLootMember groupMember in group.Members)
            {
                IPlayer memberPlayer = playerManager.GetPlayer(groupMember.Identity);
                if (memberPlayer == null)
                    return false;

                IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(groupMember.Identity);
                if (matchCharacter.Match == null || matchCharacter.Match.Status != MatchStatus.Finished)
                    return false;

                Role roles = groupMember.Identity.Equals(leader.Identity)
                    ? leaderRoles
                    : matchingDataManager.GetDefaultRole(memberPlayer.Class);

                members.Add((memberPlayer, roles));
            }

            if (members.Count < 2)
                return false;

            matchingQueueProposal = matchingQueueProposalFactory.Resolve();
            matchingQueueProposal.Initialise(leader.Faction1, matchType, matchingMaps, matchingQueueFlags);

            foreach ((IPlayer memberPlayer, Role roles) in members)
                matchingQueueProposal.AddMember(memberPlayer.Identity, roles);

            return true;
        }
    }
}
