using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching.Queue;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueMatcher : IMatchingQueueMatcher
    {
        #region Dependency Injection

        private readonly ILogger<MatchingQueueMatcher> log;
        private readonly IMatchingQueueGroupMatcher matchingQueueGroupMatcher;

        public MatchingQueueMatcher(
            ILogger<MatchingQueueMatcher> log,
            IMatchingQueueGroupMatcher matchingQueueGroupMatcher)
        {
            this.log                       = log;
            this.matchingQueueGroupMatcher = matchingQueueGroupMatcher;
        }

        #endregion

        /// <summary>
        /// Attempt to match a <see cref="IMatchingQueueProposal"/> against a list of <see cref="IMatchingQueueGroup"/>s.
        /// </summary>
        /// <remarks>
        /// A matched <see cref="IMatchingQueueGroup"/> can be an incomplete group.
        /// </remarks>
        public (IMatchingQueueGroup, IMatchingQueueGroupTeam)? Match(List<IMatchingQueueGroup> matchingQueueGroups, IMatchingQueueProposal matchingQueueProposal)
        {
            log.LogTrace($"Matching queue proposal {matchingQueueProposal.Guid} against {matchingQueueGroups.Count} matching queue groups...");

            foreach (IMatchingQueueGroup matchingQueueGroup in matchingQueueGroups
                .OrderBy(GetOldestQueueTime))
            {
                if (matchingQueueGroup.IsPaused)
                    continue;

                IMatchingQueueGroupTeam matchingQueueGroupTeam = matchingQueueGroupMatcher.Match(matchingQueueGroup, matchingQueueProposal);
                if (matchingQueueGroupTeam != null)
                {
                    log.LogTrace($"Successfully matched matching queue proposal {matchingQueueProposal.Guid} and team {matchingQueueGroupTeam.Guid} to matching queue group {matchingQueueGroup.Guid}.");
                    return (matchingQueueGroup, matchingQueueGroupTeam);
                }
            }

            return null;
        }

        private static DateTime GetOldestQueueTime(IMatchingQueueGroup matchingQueueGroup)
        {
            return matchingQueueGroup.GetTeams()
                .SelectMany(t => t.GetMembers())
                .Select(m => m.MatchingQueueProposal.QueueTime)
                .DefaultIfEmpty(DateTime.MaxValue)
                .Min();
        }
    }
}
