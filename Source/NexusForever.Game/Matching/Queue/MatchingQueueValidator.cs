using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueValidator : IMatchingQueueValidator
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IDisableManager disableManager;
        private readonly IMatchingManager matchingManager;
        private readonly IMatchingDataManager matchingDataManager;
        private readonly IMatchingRoleEnforcer matchingRoleEnforcer;
        private readonly IMatchManager matchManager;
        private readonly IMatchingDeserterManager matchingDeserterManager;

        public MatchingQueueValidator(
            IPlayerManager playerManager,
            IDisableManager disableManager,
            IMatchingManager matchingManager,
            IMatchingDataManager matchingDataManager,
            IMatchingRoleEnforcer matchingRoleEnforcer,
            IMatchManager matchManager,
            IMatchingDeserterManager matchingDeserterManager)
        {
            this.playerManager             = playerManager;
            this.disableManager            = disableManager;
            this.matchingManager           = matchingManager;
            this.matchingDataManager       = matchingDataManager;
            this.matchingRoleEnforcer      = matchingRoleEnforcer;
            this.matchManager              = matchManager;
            this.matchingDeserterManager   = matchingDeserterManager;
        }

        #endregion

        /// <summary>
        /// Determines if <see cref="IMatchingQueueProposal"/> is valid to be queued.
        /// </summary>
        public MatchingQueueResult? CanQueue(IMatchingQueueProposal matchingQueueProposal)
        {
            List<IMatchingQueueProposalMember> members = matchingQueueProposal
                .GetMembers()
                .ToList();

            foreach (IMatchingQueueProposalMember matachingQueueProposalMember in members)
            {
                IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(matachingQueueProposalMember.Identity);
                if (matchCharacter.Match != null)
                {
                    if (matchCharacter.Match.Status == MatchStatus.InProgress)
                        return MatchingQueueResult.UnableToQueue;

                    if (matchCharacter.Match.Status != MatchStatus.Finished)
                        return MatchingQueueResult.InGame;
                }

                IMatchingCharacter matchingCharacter = matchingManager.GetMatchingCharacter(matachingQueueProposalMember.Identity);
                if (matchingCharacter.GetMatchingCharacterQueue(matchingQueueProposal.MatchType) != null)
                    return MatchingQueueResult.InQueue;

                foreach (IMatchingCharacterQueue memberMatchingCharacterQueue in matchingCharacter.GetMatchingCharacterQueues())
                    if (memberMatchingCharacterQueue.MatchingQueueProposal.IsParty != matchingQueueProposal.IsParty)
                        return MatchingQueueResult.CannotQueueSoloAndGroup;
            }

            List<IPlayer> players = members
                .Select(c => playerManager.GetPlayer(c.Identity))
                .ToList();

            foreach (IPlayer player in players)
            {
                if (player.Faction1 != matchingQueueProposal.Faction)
                    return MatchingQueueResult.CannotQueueCrossFaction;

                if (!matchingDeserterManager.CanQueue(player.CharacterId, matchingQueueProposal.MatchType))
                {
                    matchingDeserterManager.SyncDeserterUi(player);
                    return MatchingQueueResult.UnableToQueue;
                }
            }

            MatchingQueueResult? warplotResult = RetailWarplotQueueRules.ValidateQueue(
                matchingQueueProposal, playerManager, matchingManager);
            if (warplotResult != null)
                return warplotResult;

            if (matchingQueueProposal.IsParty
                && members.Any(m => matchManager.GetMatchCharacter(m.Identity).Match?.Status == MatchStatus.Finished)
                && !matchingDataManager.CanRequeueAsGroup(matchingQueueProposal.MatchType))
                return MatchingQueueResult.InvalidRequeueType;

            if (matchingDataManager.IsCompositionEnforced(matchingQueueProposal.MatchType))
                if (!matchingRoleEnforcer.Check(members).Success)
                    return MatchingQueueResult.GroupSize;

            IEnumerable<IMatchingMap> matchingMaps = matchingQueueProposal.GetMatchingMaps();
            if (!matchingMaps.Any())
                return MatchingQueueResult.GroupMemberMatching;

            foreach (IMatchingMap matchingMap in matchingMaps)
            {
                if (disableManager.IsDisabled(DisableType.World, matchingMap.GameMapEntry.WorldId))
                    return MatchingQueueResult.UnableToQueue;

                if (matchingDataManager.GetMapEntrance(matchingMap.GameMapEntry.WorldId, 0) == null)
                    return MatchingQueueResult.UnableToQueue;

                if (matchingMap.GameTypeEntry.MatchTypeEnum != matchingQueueProposal.MatchType)
                    return MatchingQueueResult.TypeMismatch;

                if (matchingMap.GameTypeEntry.TeamSize < players.Count)
                    return MatchingQueueResult.GroupSize;

                if (players.Any(m => m.Level < matchingMap.GameTypeEntry.MinLevel || m.Level > matchingMap.GameTypeEntry.MaxLevel))
                    return MatchingQueueResult.Level;
            }

            return null;
        }
    }
}
