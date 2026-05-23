using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchCastVoteKickHandler : IMessageHandler<IWorldSession, ClientMatchingMatchCastVoteKick>
    {
        private readonly ILogger<ClientMatchingMatchCastVoteKickHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingMatchCastVoteKickHandler(
            ILogger<ClientMatchingMatchCastVoteKickHandler> log,
            IMatchManager matchManager)
        {
            this.log           = log;
            this.matchManager = matchManager;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchCastVoteKick castVoteKick)
        {
            if (session.Player == null)
                return;

            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(session.Player.Identity);
            if (matchCharacter.Match is not Match match)
                return;

            MatchingQueueResult? result = match.CastVoteKick(session.Player, castVoteKick.MemberToKick.ToGameIdentity(), castVoteKick.Vote);
            if (result != null)
            {
                session.EnqueueMessageEncrypted(new ServerMatchingMatchVoteKickFailed());
                log.LogDebug("Vote kick cast rejected for player {PlayerGuid}: {Result}.", session.Player.Guid, result);
            }
        }
    }
}
