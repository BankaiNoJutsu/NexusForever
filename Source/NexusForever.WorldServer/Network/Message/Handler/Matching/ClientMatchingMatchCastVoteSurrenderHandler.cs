using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchCastVoteSurrenderHandler : IMessageHandler<IWorldSession, ClientMatchingMatchCastVoteSurrender>
    {
        private readonly ILogger<ClientMatchingMatchCastVoteSurrenderHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingMatchCastVoteSurrenderHandler(
            ILogger<ClientMatchingMatchCastVoteSurrenderHandler> log,
            IMatchManager matchManager)
        {
            this.log           = log;
            this.matchManager = matchManager;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchCastVoteSurrender castVoteSurrender)
        {
            if (session.Player == null)
                return;

            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(session.Player.Identity);
            if (matchCharacter.Match is not PvpMatch match)
                return;

            MatchingQueueResult? result = match.CastVoteSurrender(session.Player, castVoteSurrender.Vote);
            if (result != null)
                log.LogDebug("Surrender vote cast rejected for player {PlayerGuid}: {Result}.", session.Player.Guid, result);
        }
    }
}
