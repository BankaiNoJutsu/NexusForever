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

            // Validate player is in a match
            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(player.Identity);
            IMatch match = matchCharacter?.Match;
            if (match == null)
            {
                log.LogWarning("ClientMatchingStopLookingForReplacements: player {Player} is not in a match.", player.Guid);
                return;
            }

            log.LogInformation("ClientMatchingStopLookingForReplacements: player={Player}, match={Match}",
                player.Guid, match.Guid);

            // When the full replacement flow is implemented, this would:
            // 1. Find the player's matching queue group
            // 2. Clear the InProgress flag
            // 3. Remove the group from the matching queue if it was registered for replacements
            // 4. Broadcast status to all match members
        }
    }
}
