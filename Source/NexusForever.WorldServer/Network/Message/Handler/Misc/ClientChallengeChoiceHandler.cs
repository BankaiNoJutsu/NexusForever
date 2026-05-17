using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientChallengeChoiceHandler : IMessageHandler<IWorldSession, ClientChallengeChoice>
    {
        private readonly ILogger<ClientChallengeChoiceHandler> log;

        public ClientChallengeChoiceHandler(ILogger<ClientChallengeChoiceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientChallengeChoice challengeChoice)
        {
            log.LogDebug("ClientChallengeChoice: player={Player} challengeId={ChallengeId} choice={Choice}",
                session.Player?.Guid, challengeChoice.ChallengeId, challengeChoice.Choice);
        }
    }
}
