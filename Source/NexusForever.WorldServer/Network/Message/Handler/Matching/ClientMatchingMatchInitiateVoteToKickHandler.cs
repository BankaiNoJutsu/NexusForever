using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateVoteToKickHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateVoteToKick>
    {
        private readonly ILogger<ClientMatchingMatchInitiateVoteToKickHandler> log;
        private readonly IMatchManager matchManager;

        public ClientMatchingMatchInitiateVoteToKickHandler(
            ILogger<ClientMatchingMatchInitiateVoteToKickHandler> log,
            IMatchManager matchManager)
        {
            this.log           = log;
            this.matchManager = matchManager;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateVoteToKick initiateVoteToKick)
        {
            if (session.Player == null)
                return;

            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(session.Player.Identity);
            if (matchCharacter.Match is not Match match)
                return;

            MatchingQueueResult? result = match.TryInitiateVoteKick(session.Player, initiateVoteToKick.MemberToKick.ToGameIdentity());
            if (result != null)
            {
                if (result != MatchingQueueResult.PersonalKickCooldown)
                    session.EnqueueMessageEncrypted(new ServerMatchingMatchVoteKickFailed());

                log.LogDebug("Vote kick initiate rejected for player {PlayerGuid}: {Result}.", session.Player.Guid, result);
            }
        }
    }
}
