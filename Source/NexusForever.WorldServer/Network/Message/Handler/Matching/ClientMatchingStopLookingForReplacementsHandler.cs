using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingStopLookingForReplacementsHandler : IMessageHandler<IWorldSession, ClientMatchingStopLookingForReplacements>
    {
        #region Dependency Injection

        private readonly ILogger<ClientMatchingStopLookingForReplacementsHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingStopLookingForReplacementsHandler(
            ILogger<ClientMatchingStopLookingForReplacementsHandler> log,
            IMatchManager matchManager)
        {
            this.log          = log;
            this.matchManager = matchManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientMatchingStopLookingForReplacements stopLookingForReplacements)
        {
            IPlayer player = session.Player;
            if (player == null)
                return;

            if (!MatchingLookingForReplacementsValidation.TryGetInProgressMatch(matchManager, player, out IMatch match))
            {
                log.LogWarning("ClientMatchingStopLookingForReplacements: player {Player} is not in an in-progress match.", player.Guid);
                return;
            }

            log.LogInformation("ClientMatchingStopLookingForReplacements: player={Player}, match={Match}",
                player.Guid, match.Guid);
        }
    }
}
