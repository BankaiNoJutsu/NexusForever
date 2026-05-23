using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueGroupMatcher : IMatchingQueueGroupMatcher
    {
        #region Dependency Injection

        private readonly ILogger<MatchingQueueGroupMatcher> log;

        private readonly IMatchingDataManager matchingDataManager; 
        private readonly IMatchingRoleEnforcer matchingRoleEnforcer;
        private readonly IPlayerManager playerManager;
        public MatchingQueueGroupMatcher(
            ILogger<MatchingQueueGroupMatcher> log,
            IMatchingDataManager matchingDataManager,
            IMatchingRoleEnforcer matchingRoleEnforcer,
            IPlayerManager playerManager)
        {
            this.log                         = log;

            this.matchingDataManager         = matchingDataManager;
            this.matchingRoleEnforcer        = matchingRoleEnforcer;
            this.playerManager               = playerManager;
        }

        #endregion

        /// <summary>
        /// Attempt to match <see cref="IMatchingQueueProposal"/> against <see cref="IMatchingQueueGroup"/>.
        /// </summary>
        public IMatchingQueueGroupTeam Match(IMatchingQueueGroup matchingQueueGroup, IMatchingQueueProposal matchingQueueProposal)
        {
            log.LogTrace($"Attempting to match matching queue proposal {matchingQueueProposal.Guid} against matching queue group {matchingQueueGroup.Guid}.");

            IEnumerable<IMatchingMap> commonMatchingMaps = matchingQueueGroup.GetMatchingMaps()
                .Intersect(matchingQueueProposal.GetMatchingMaps());

            if (!commonMatchingMaps.Any())
                return null;

            foreach (IMatchingQueueGroupTeam matchingQueueGroupTeam in matchingQueueGroup.GetTeams()
                .OrderBy(GetOldestQueueTime))
                if (Match(commonMatchingMaps, matchingQueueGroupTeam, matchingQueueProposal))
                    return matchingQueueGroupTeam;

            return null;
        }

        private static DateTime GetOldestQueueTime(IMatchingQueueGroupTeam matchingQueueGroupTeam)
        {
            return matchingQueueGroupTeam.GetMembers()
                .Select(m => m.MatchingQueueProposal.QueueTime)
                .DefaultIfEmpty(DateTime.MaxValue)
                .Min();
        }

        private bool Match(IEnumerable<IMatchingMap> commonMatchingMaps, IMatchingQueueGroupTeam matchingQueueGroupTeam, IMatchingQueueProposal matchingQueueProposal)
        {
            if (matchingDataManager.IsSingleFactionEnforced(matchingQueueProposal.MatchType))
                if (matchingQueueGroupTeam.Faction != matchingQueueProposal.Faction)
                    return false;

            if (!IsRealmMatchAllowed(matchingQueueGroupTeam, matchingQueueProposal))
                return false;

            List<IMatchingQueueProposalMember> matchingQueueProposalMembers = matchingQueueGroupTeam
                .GetMembers()
                .Concat(matchingQueueProposal.GetMembers())
                .ToList();

            foreach (IMatchingMap matchingMap in commonMatchingMaps)
            {
                if (Match(matchingMap, matchingQueueProposalMembers))
                    return true;
            }

            return false;
        }

        private bool Match(IMatchingMap matchingMap, List<IMatchingQueueProposalMember> matchingQueueProposalMembers)
        {
            if (matchingQueueProposalMembers.Count > matchingMap.GameTypeEntry.TeamSize)
                return false;

            if (matchingDataManager.RequiresRoleSelection(matchingMap.GameTypeEntry.MatchTypeEnum))
            {
                IMatchingRoleEnforcerResult result = matchingRoleEnforcer.Check(matchingQueueProposalMembers);
                if (!result.Success)
                    return false;
            }

            return true;
        }

        private bool IsRealmMatchAllowed(IMatchingQueueGroupTeam matchingQueueGroupTeam, IMatchingQueueProposal matchingQueueProposal)
        {
            if (!RetailMatchingRealmRules.RequiresSameRealm(matchingQueueProposal))
                return true;

            ushort? requiredRealm = null;
            foreach (IMatchingQueueProposalMember member in matchingQueueProposal.GetMembers())
            {
                IPlayer player = playerManager.GetPlayer(member.Identity);
                if (player == null)
                    return false;

                requiredRealm ??= player.Identity.RealmId;
                if (player.Identity.RealmId != requiredRealm)
                    return false;
            }

            foreach (IMatchingQueueProposalMember member in matchingQueueGroupTeam.GetMembers())
            {
                IPlayer player = playerManager.GetPlayer(member.Identity);
                if (player == null || player.Identity.RealmId != requiredRealm)
                    return false;
            }

            return true;
        }
    }
}
