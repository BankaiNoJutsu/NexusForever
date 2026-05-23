using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateVoteToSurrenderHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateVoteToSurrender>
    {
        private readonly ILogger<ClientMatchingMatchInitiateVoteToSurrenderHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingMatchInitiateVoteToSurrenderHandler(
            ILogger<ClientMatchingMatchInitiateVoteToSurrenderHandler> log,
            IMatchManager matchManager)
        {
            this.log           = log;
            this.matchManager = matchManager;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateVoteToSurrender _)
        {
            if (session.Player == null)
                return;

            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(session.Player.Identity);
            if (matchCharacter.Match is not PvpMatch match)
                return;

            MatchingQueueResult? result = match.TryInitiateVoteSurrender(session.Player);
            if (result != null)
            {
                if (result != MatchingQueueResult.PersonalSurrenderCooldown)
                    session.EnqueueMessageEncrypted(new ServerMatchingMatchVoteSurrenderFailed());

                log.LogDebug("Surrender vote initiate rejected for player {PlayerGuid}: {Result}.", session.Player.Guid, result);
            }
        }
    }
}
