using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Matching;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingManager : IMatchingManager
    {
        private IMatchingQueueManager pveMatchingQueueManager;
        private IMatchingQueueManager pvpMatchingQueueManager;

        private readonly ConcurrentQueue<IMatchingQueueProposal> incomingMatchingQueueProposals = [];

        private readonly Dictionary<Identity, IMatchingCharacter> characters = [];

        private readonly List<IMatchingRoleCheck> matchingRoleChecks = [];
        private readonly Dictionary<Identity, IMatchingRoleCheck> characterMatchingRoleChecks = [];

        #region Dependency Injection

        private readonly ILogger<MatchingManager> log;

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IMatchingQueueTimeManager matchingQueueTimeManager;
        private readonly IFactory<IMatchingQueueManager> matchingQueueManagerFactory;
        private readonly IFactory<IMatchingQueueProposal> matchingQueueProposalFactory;
        private readonly IFactory<IMatchingRoleCheck> matchingRoleCheckFactory;
        private readonly IFactory<IMatchingCharacter> matchingCharacterFactory;
        private readonly IGroupStateManager groupStateManager;
        private readonly IPlayerManager playerManager;
        private readonly IMatchCharacterStore matchCharacterStore;
        private readonly IMatchingDeserterManager matchingDeserterManager;
        public MatchingManager(
            ILogger<MatchingManager> log,
            IMatchingDataManager matchingDataManager,
            IMatchingQueueTimeManager matchingQueueTimeManager,
            IFactory<IMatchingQueueManager> matchingQueueManagerFactory,
            IFactory<IMatchingQueueProposal> matchingQueueProposalFactory,
            IFactory<IMatchingRoleCheck> matchingRoleCheckFactory,
            IFactory<IMatchingCharacter> matchingCharacterFactory,
            IGroupStateManager groupStateManager,
            IPlayerManager playerManager,
            IMatchCharacterStore matchCharacterStore,
            IMatchingDeserterManager matchingDeserterManager)
        {
            this.log                          = log;
            this.matchingDataManager          = matchingDataManager;
            this.matchingQueueTimeManager     = matchingQueueTimeManager;
            this.matchingQueueManagerFactory  = matchingQueueManagerFactory;
            this.matchingQueueProposalFactory = matchingQueueProposalFactory;
            this.matchingRoleCheckFactory     = matchingRoleCheckFactory;
            this.matchingCharacterFactory     = matchingCharacterFactory;
            this.groupStateManager            = groupStateManager;
            this.playerManager                = playerManager;
            this.matchCharacterStore          = matchCharacterStore;
            this.matchingDeserterManager      = matchingDeserterManager;
        }

        #endregion

        /// <summary>
        /// Initialise data and queue managers for <see cref="IMatchingManager"/>.
        /// </summary>
        public void Initialise()
        {
            if (pveMatchingQueueManager != null)
                throw new InvalidOperationException();

            matchingDataManager.Initialise();
            matchingQueueTimeManager.Initialise();

            pveMatchingQueueManager = CreateMatchingQueueManager();
            pvpMatchingQueueManager = CreateMatchingQueueManager();
        }

        private IMatchingQueueManager CreateMatchingQueueManager()
        {
            IMatchingQueueManager matchingQueueManager = matchingQueueManagerFactory.Resolve();
            matchingQueueManager.Initialise();
            return matchingQueueManager;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            pveMatchingQueueManager.Update(lastTick);
            pvpMatchingQueueManager.Update(lastTick);

            UpdateIncomingProposals();
            UpdateRoleChecks(lastTick);
        }

        private void UpdateIncomingProposals()
        {
            if (!incomingMatchingQueueProposals.TryDequeue(out IMatchingQueueProposal matchingQueueProposal))
                return;

            JoinQueue(matchingQueueProposal);
        }

        private void UpdateRoleChecks(double lastTick)
        {
            if (matchingRoleChecks.Count > 0)
            {
                var matchingRoleChecksToRemove = new List<IMatchingRoleCheck>();
                foreach (IMatchingRoleCheck matchingRoleCheck in matchingRoleChecks)
                {
                    matchingRoleCheck.Update(lastTick);
                    switch (matchingRoleCheck.Status)
                    {
                        case MatchingRoleCheckStatus.Declined:
                        {
                            log.LogTrace($"Role check {matchingRoleCheck.Guid} was declined.");
                            matchingRoleChecksToRemove.Add(matchingRoleCheck);
                            break;
                        }
                        case MatchingRoleCheckStatus.Expired:
                        {
                            log.LogTrace($"Role check {matchingRoleCheck.Guid} has expired.");
                            matchingRoleChecksToRemove.Add(matchingRoleCheck);
                            break;
                        }
                        case MatchingRoleCheckStatus.Success:
                        {
                            MatchingRoleCheckSuccessful(matchingRoleCheck);
                            matchingRoleChecksToRemove.Add(matchingRoleCheck);
                            break;
                        }
                    }
                }

                foreach (IMatchingRoleCheck matchingRoleCheck in matchingRoleChecksToRemove)
                {
                    matchingRoleChecks.Remove(matchingRoleCheck);
                    foreach (IMatchingRoleCheckMember matchingRoleCheckMember in matchingRoleCheck.GetMembers())
                    {
                        characterMatchingRoleChecks.Remove(matchingRoleCheckMember.Identity);
                        GetMatchingCharacter(matchingRoleCheckMember.Identity).SendMatchingStatus();
                    }

                    log.LogTrace($"Role check {matchingRoleCheck.Guid} removed from store.");
                }
            }
        }

        private void MatchingRoleCheckSuccessful(IMatchingRoleCheck matchingRoleCheck)
        {
            foreach (IMatchingRoleCheckMember matchingRoleCheckMember in matchingRoleCheck.GetMembers())
            {
                matchingRoleCheck.MatchingQueueProposal.AddMember(matchingRoleCheckMember.Identity, matchingRoleCheckMember.Roles.Value);

                // remove member from all solo queues
                foreach (IMatchingCharacterQueue matchingCharacterQueue in GetMatchingCharacter(matchingRoleCheckMember.Identity).GetMatchingCharacterQueues().ToList())
                    if (!matchingCharacterQueue.MatchingQueueProposal.IsParty)
                        matchingCharacterQueue.MatchingQueueGroup.RemoveMatchingQueueProposal(matchingCharacterQueue.MatchingQueueProposal);
            }

            log.LogTrace($"Role check {matchingRoleCheck.Guid} was successful, adding match proposal {matchingRoleCheck.MatchingQueueProposal.Guid} to queue.");
            JoinQueue(matchingRoleCheck.MatchingQueueProposal);
        }

        /// <summary>
        /// Return <see cref="IMatchingCharacter"/> for supplied character id.
        /// </summary>
        /// <remarks>
        /// Will return a new <see cref="IMatchingCharacter"/> if one does not exist.
        /// </remarks>
        public IMatchingCharacter GetMatchingCharacter(Identity identity)
        {
            if (!characters.TryGetValue(identity, out IMatchingCharacter characterInfo))
            {
                characterInfo = matchingCharacterFactory.Resolve();
                characterInfo.Initialise(identity);
                characters.Add(identity, characterInfo);
            }

            return characterInfo;
        }

        /// <summary>
        /// Return <see cref="IMatchingRoleCheck"/> for supplied character id.
        /// </summary>
        public IMatchingRoleCheck GetMatchingRoleCheck(Identity identity)
        {
            return characterMatchingRoleChecks.TryGetValue(identity, out IMatchingRoleCheck matchingRoleCheck) ? matchingRoleCheck : null;
        }

        public Static.Matching.MatchType GetReadyMatchType(Identity identity)
        {
            IMatchingRoleCheck matchingRoleCheck = GetMatchingRoleCheck(identity);
            return matchingRoleCheck?.Status == MatchingRoleCheckStatus.Pending
                ? matchingRoleCheck.MatchingQueueProposal.MatchType
                : Static.Matching.MatchType.None;
        }

        /// <summary>
        /// Attempt to join a matching queue.
        /// </summary>
        public void JoinQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, List<uint> maps, uint matchingGameTypeId, MatchingQueueFlags matchingQueueFlags)
        {
            log.LogTrace($"Queue join request, Character: {player.Identity}, Roles: {roles}, MatchType: {matchType}, Maps: {string.Join(", ", maps)}, Type {matchingGameTypeId}, Flags: {matchingQueueFlags}.");

            List<IMatchingMap> matchingMaps = GetMatchingMaps(maps, matchingGameTypeId);
            JoinQueue(player, roles, matchType, matchingMaps, matchingQueueFlags);
        }

        /// <summary>
        /// Attempt to join a matching queue with a party.
        /// </summary>
        public void JoinPartyQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, List<uint> maps, uint matchingGameTypeId, MatchingQueueFlags matchingQueueFlags)
        {
            log.LogTrace($"Party queue join request, Character: {player.Identity}, Roles: {roles}, MatchType: {matchType}, Maps: {string.Join(", ", maps)}, Type {matchingGameTypeId}, Flags: {matchingQueueFlags}.");

            List<IMatchingMap> matchingMaps = GetMatchingMaps(maps, matchingGameTypeId);
            JoinPartyQueue(player, roles, matchType, matchingMaps, matchingQueueFlags);
        }

        private List<IMatchingMap> GetMatchingMaps(List<uint> maps, uint matchingGameTypeId)
        {
            // arenas also specify the MatchingGameType
            // this is because the MatchType is not enough to determine the type (1v1, 3v3, 5v5)
            if (matchingGameTypeId != 0)
                return matchingDataManager.GetMatchingMaps(matchingGameTypeId)
                    .ToList();

            return maps
                .Select(matchingDataManager.GetMatchingMap)
                .ToList();
        }

        private void JoinQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, List<IMatchingMap> matchingMaps, MatchingQueueFlags matchingQueueFlags)
        {
            IMatchingQueueProposal matchingQueueProposal = matchingQueueProposalFactory.Resolve();
            matchingQueueProposal.Initialise(player.Faction1, matchType, matchingMaps, matchingQueueFlags);
            matchingQueueProposal.AddMember(player.Identity, roles);
            incomingMatchingQueueProposals.Enqueue(matchingQueueProposal);
        }

        private void JoinPartyQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, List<IMatchingMap> matchingMaps, MatchingQueueFlags matchingQueueFlags)
        {
            MatchingQueueResult? leaderResult = RetailPartyQueueRules.ValidateGroupLeader(player, groupStateManager);
            if (leaderResult != null)
            {
                player.Session?.EnqueueMessageEncrypted(new ServerMatchingQueueResultAnnounce
                {
                    Result = leaderResult.Value
                });
                return;
            }

            if (RetailPartyQueueRules.TryCreateFinishedGroupRequeueProposal(
                    player,
                    roles,
                    matchType,
                    matchingMaps,
                    matchingQueueFlags,
                    groupStateManager,
                    playerManager,
                        matchCharacterStore,
                    matchingDataManager,
                    matchingQueueProposalFactory,
                    out IMatchingQueueProposal requeueProposal))
            {
                requeueProposal.Broadcast(new ServerMatchingQueueResultAnnounce
                {
                    Result = MatchingQueueResult.Requeueing
                });

                log.LogTrace($"Group requeue proposal {requeueProposal.Guid} created for leader {player.Identity}.");
                JoinQueue(requeueProposal);
                return;
            }

            IMatchingQueueProposal matchingQueueProposal = matchingQueueProposalFactory.Resolve();
            matchingQueueProposal.Initialise(player.Faction1, matchType, matchingMaps, matchingQueueFlags);

            IEnumerable<IPlayer> nearbyPlayers = player.Map
                .Search(player.Position, 10f, new SearchCheckRange<IPlayer>(player.Position, 10f));

            List<Identity> identities = RetailPartyQueueRules.ResolvePartyMemberIdentities(
                player,
                groupStateManager,
                nearbyPlayers);

            IMatchingRoleCheck matchingRoleCheck = matchingRoleCheckFactory.Resolve();
            matchingRoleCheck.Initialise(matchingQueueProposal, identities);

            matchingRoleChecks.Add(matchingRoleCheck);
            foreach (Identity identity in identities)
                characterMatchingRoleChecks.Add(identity, matchingRoleCheck);

            foreach (Identity identity in identities)
                GetMatchingCharacter(identity).SendMatchingStatus();

            log.LogTrace($"Role check {matchingRoleCheck.Guid} added to store.");
        }

        private void JoinQueue(IMatchingQueueProposal matchingQueueProposal)
        {
            IMatchingQueueManager matchingQueueManager = GetMatchingQueueManager(matchingQueueProposal.MatchType);

            MatchingQueueResult? matchingResult = matchingQueueManager.CanQueue(matchingQueueProposal);
            if (matchingResult != null)
            {
                matchingQueueProposal.Broadcast(new ServerMatchingQueueResultAnnounce
                {
                    Result = matchingResult.Value
                });

                log.LogTrace($"Matching queue proposal {matchingQueueProposal.Guid} failed to validate, reason: {matchingResult}.");
                return;
            }

            matchingQueueManager.JoinQueue(matchingQueueProposal);
        }

        private IMatchingQueueManager GetMatchingQueueManager(Static.Matching.MatchType matchType)
        {
            if (matchingDataManager.IsPvPMatchType(matchType))
                return pvpMatchingQueueManager;

            return pveMatchingQueueManager;
        }

        /// <summary>
        /// Attempt to join a random queue.
        /// </summary>
        public void JoinRandomQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType)
        {
            JoinRandomQueue(player, roles, matchType, MatchingQueueFlags.None);
        }

        public void JoinRandomQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, MatchingQueueFlags matchingQueueFlags)
        {
            log.LogTrace($"Random queue join request, Character: {player.Identity}, Roles: {roles}, MatchType:, {matchType}, Flags: {matchingQueueFlags}.");

            List<IMatchingMap> maps = matchingDataManager.GetMatchingMaps(matchType).ToList();
            JoinQueue(player, roles, matchType, maps, matchingQueueFlags);
        }

        /// <summary>
        /// Attempt to join a random party queue.
        /// </summary>
        public void JoinRandomPartyQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType)
        {
            JoinRandomPartyQueue(player, roles, matchType, MatchingQueueFlags.None);
        }

        public void JoinRandomPartyQueue(IPlayer player, Role roles, Static.Matching.MatchType matchType, MatchingQueueFlags matchingQueueFlags)
        {
            log.LogTrace($"Random party queue join request, Character: {player.Identity}, Roles: {roles}, MatchType:, {matchType}, Flags: {matchingQueueFlags}.");

            List<IMatchingMap> maps = matchingDataManager.GetMatchingMaps(matchType).ToList();
            JoinPartyQueue(player, roles, matchType, maps, matchingQueueFlags);
        }

        /// <summary>
        /// Remove <see cref="IPlayer"/> from specified <see cref="Static.Matching.MatchType"/> queue.
        /// </summary>
        public void LeaveQueue(IPlayer player, Static.Matching.MatchType matchType)
        {
            if (player == null)
                return;

            log.LogTrace($"Leave queue request, Character: {player.Identity}, MatchType: {matchType}.");

            IMatchingCharacter character = GetMatchingCharacter(player.Identity);
            IMatchingCharacterQueue matchingCharacterQueue = character.GetMatchingCharacterQueue(matchType);
            matchingCharacterQueue?.MatchingQueueGroup.RemoveMatchingQueueProposal(matchingCharacterQueue.MatchingQueueProposal);
        }

        /// <summary>
        /// Remove <see cref="IPlayer"/> from all queues.
        /// </summary>
        public void LeaveQueue(IPlayer player)
        {
            if (player == null)
                return;

            log.LogTrace($"Leave queue request, Character: {player.Identity}.");

            IMatchingCharacter character = GetMatchingCharacter(player.Identity);
            foreach (IMatchingCharacterQueue matchingCharacterQueue in character.GetMatchingCharacterQueues().ToList())
                matchingCharacterQueue.MatchingQueueGroup.RemoveMatchingQueueProposal(matchingCharacterQueue.MatchingQueueProposal);
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> logs in.
        /// </summary>
        public void BroadcastAverageWaitTimeUpdate(Static.Matching.MatchType matchType)
        {
            if (matchType == Static.Matching.MatchType.None)
                return;

            uint averageWaitTimeMs = (uint)matchingQueueTimeManager.GetAverageWaitTime(matchType).TotalMilliseconds;
            foreach (IMatchingCharacter matchingCharacter in characters.Values)
                matchingCharacter.SendAverageWaitTimeUpdate(matchType, averageWaitTimeMs);
        }

        public void OnLogin(IPlayer player)
        {
            IMatchingCharacter matchingCharacter = GetMatchingCharacter(player.Identity);
            matchingDeserterManager.RestoreDeserter(player);
            matchingCharacter.SendMatchingStatus();
            matchingDeserterManager.SyncDeserterUi(player);

            foreach (IMatchingCharacterQueue matchingCharacterQueue in matchingCharacter.GetMatchingCharacterQueues())
            {
                Static.Matching.MatchType matchType = matchingCharacterQueue.MatchingQueueProposal.MatchType;
                uint averageWaitTimeMs = (uint)matchingQueueTimeManager.GetAverageWaitTime(matchType).TotalMilliseconds;
                matchingCharacter.SendAverageWaitTimeUpdate(matchType, averageWaitTimeMs);
            }
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> goes offline.
        /// </summary>
        public void OnLogout(IPlayer player)
        {
            GetMatchingRoleCheck(player.Identity)?.Respond(player.Identity, Role.None);
        }
    }
}
