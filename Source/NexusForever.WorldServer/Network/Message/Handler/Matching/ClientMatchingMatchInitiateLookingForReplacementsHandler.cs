using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateLookingForReplacementsHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateLookingForReplacements>
    {
        #region Dependency Injection

        private readonly ILogger<ClientMatchingMatchInitiateLookingForReplacementsHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingMatchInitiateLookingForReplacementsHandler(
            ILogger<ClientMatchingMatchInitiateLookingForReplacementsHandler> log,
            IMatchManager matchManager)
        {
            this.log          = log;
            this.matchManager = matchManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateLookingForReplacements initiateLookingForReplacements)
        {
            IPlayer player = session.Player;
            if (player == null)
                return;

            // 1. Validate player is in a match
            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(player.Identity);
            IMatch match = matchCharacter?.Match;
            if (match == null)
            {
                log.LogWarning("ClientMatchingMatchInitiateLookingForReplacements: player {Player} is not in a match.", player.Guid);
                return;
            }

            // 2. Validate match is in progress
            if (match.Status != MatchStatus.InProgress)
            {
                log.LogWarning("ClientMatchingMatchInitiateLookingForReplacements: player {Player} match {Match} status is {Status}, expected InProgress.",
                    player.Guid, match.Guid, match.Status);
                return;
            }

            Role requestedRoles = initiateLookingForReplacements.Roles;
            log.LogInformation("ClientMatchingMatchInitiateLookingForReplacements: player={Player}, match={Match}, roles={Roles}",
                player.Guid, match.Guid, requestedRoles);

            // The full replacement flow requires:
            // 1. Creating a matching queue proposal with InProgress=true on the queue group
            // 2. The matching queue then matches solo queuers into the existing match
            // 3. Upon match, sends ServerMatchingMatchInProgressReady instead of ServerMatchingMatchReady
            //
            // This infrastructure is partially implemented:
            // - MatchingQueueGroup.InProgress flag and SetInProgress() method exist
            // - MatchProposal.SendMatchReady checks InProgress for correct packet type
            // - F-010 in MISSING_FEATURE_MATRIX.md tracks the remaining gap
        }
    }
}
