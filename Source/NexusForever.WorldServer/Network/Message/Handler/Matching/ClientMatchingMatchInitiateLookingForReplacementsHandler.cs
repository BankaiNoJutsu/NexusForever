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

            if (!MatchingLookingForReplacementsValidation.TryGetInProgressMatch(matchManager, player, out IMatch match))
            {
                log.LogWarning("ClientMatchingMatchInitiateLookingForReplacements: player {Player} is not in an in-progress match.", player.Guid);
                return;
            }

            Role requestedRoles = initiateLookingForReplacements.Roles;
            if (!MatchingLookingForReplacementsValidation.IsValidReplacementRoleMask(requestedRoles))
            {
                log.LogWarning("ClientMatchingMatchInitiateLookingForReplacements: player={Player}, match={Match}, invalid role mask={Roles}",
                    player.Guid, match.Guid, requestedRoles);
                return;
            }

            log.LogInformation("ClientMatchingMatchInitiateLookingForReplacements: player={Player}, match={Match}, roles={Roles}",
                player.Guid, match.Guid, requestedRoles);
        }
    }
}
