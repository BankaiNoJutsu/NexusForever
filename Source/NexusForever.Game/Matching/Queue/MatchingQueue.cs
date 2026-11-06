using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Shared;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueue : IMatchingQueue
    {
        private Static.Matching.MatchType matchType = Static.Matching.MatchType.None;
        private readonly List<IMatchingQueueGroup> matchingGroups = [];

        #region Dependency Injection

        private readonly ILogger<MatchingQueue> log;

        private readonly IMatchingQueueMatcher matchingQueueMatcher;
        private readonly IMatchingMapSelector matchingMapSelector;
        private readonly IFactory<IMatchingQueueGroup> matchingQueueGroupFactory;
        private readonly IMatchManager matchManager;

        public MatchingQueue(
            ILogger<MatchingQueue> log,
            IMatchingQueueMatcher matchingQueueMatcher,
            IMatchingMapSelector matchingMapSelector,
            IFactory<IMatchingQueueGroup> matchingQueueGroupFactory,
            IMatchManager matchManager)
        {
            this.log                       = log;

            this.matchingQueueMatcher      = matchingQueueMatcher;
            this.matchingMapSelector       = matchingMapSelector;
            this.matchingQueueGroupFactory = matchingQueueGroupFactory;
            this.matchManager              = matchManager;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IMatchingQueue"/> with supplied <see cref="Static.Matching.MatchType"/>.
        /// </summary>
        public void Initialise(Static.Matching.MatchType matchType)
        {
            if (this.matchType != Static.Matching.MatchType.None)
                throw new InvalidOperationException();

            this.matchType = matchType;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (matchingGroups.Count == 0)
                return;

            var matchingGroupsToRemove = new List<IMatchingQueueGroup>();
            foreach (IMatchingQueueGroup matchingQueueGroup in matchingGroups)
                if (matchingQueueGroup.IsFinalised)
                    matchingGroupsToRemove.Add(matchingQueueGroup);

            foreach (IMatchingQueueGroup matchingQueueGroup in matchingGroupsToRemove)
            {
                matchingGroups.Remove(matchingQueueGroup);
                log.LogTrace($"Matching queue group {matchingQueueGroup.Guid} removed from store.");
            }
        }

        /// <summary>
        /// Attempt to add <see cref="IMatchingQueueProposal"/> to the queue.
        /// </summary>
        /// <remarks>
        /// An attempt to match the <see cref="IMatchingQueueProposal"/> with any <see cref="IMatchingQueueGroup"/>s already in queue, otherwise add to queue.
        /// </remarks>
        public void JoinQueue(IMatchingQueueProposal matchingQueueProposal)
        {
            if (matchingQueueProposal.InstantQueue)
            {
                log.LogTrace($"Matching queue proposal {matchingQueueProposal.Guid} is a solo group, matched right away.");

                IMatchingQueueGroup newMatchingQueueGroup = AddToQueue(matchingQueueProposal);
                if (newMatchingQueueGroup.IsFinalised || newMatchingQueueGroup.GetMatchingMaps().Count == 0)
                    return;
                Matched(newMatchingQueueGroup, new MatchingMapSelectorResult
                {
                    MatchingMap = newMatchingQueueGroup.GetMatchingMaps()[Random.Shared.Next(newMatchingQueueGroup.GetMatchingMaps().Count)]
                });
                return;
            }

            (IMatchingQueueGroup MatchingQueueGroup, IMatchingQueueGroupTeam MatchingQueueGroupTeam)? matchDetails = matchingQueueMatcher.Match(matchingGroups, matchingQueueProposal);
            if (matchDetails != null)
            {
                IMatchingQueueGroup matchingQueueGroup = matchDetails.Value.MatchingQueueGroup;
                matchingQueueGroup.AddMatchingQueueProposal(matchingQueueProposal, matchDetails.Value.MatchingQueueGroupTeam);

                if (matchingQueueGroup.IsFinalised || matchingQueueGroup.IsPaused)
                    return;

                IMatchingMapSelectorResult matchingMapSelectorResult = matchingMapSelector.Select(matchingQueueGroup);
                if (matchingMapSelectorResult != null)
                    Matched(matchingQueueGroup, matchingMapSelectorResult);

                return;
            }

            AddToQueue(matchingQueueProposal);
        }

        private void Matched(IMatchingQueueGroup matchingQueueGroup, IMatchingMapSelectorResult matchingMapSelectorResult)
        {
            matchManager.CreateMatchProposal(matchingQueueGroup, matchingMapSelectorResult);
        }

        private IMatchingQueueGroup AddToQueue(IMatchingQueueProposal matchingQueueProposal)
        {
            IMatchingQueueGroup matchingGroup = matchingQueueGroupFactory.Resolve();
            matchingGroup.Initialise(matchingQueueProposal);
            if (!matchingGroup.IsFinalised)
                matchingGroups.Add(matchingGroup);

            log.LogTrace($"Matching queue group {matchingGroup.Guid} added to store.");

            return matchingGroup;
        }

    }
}
