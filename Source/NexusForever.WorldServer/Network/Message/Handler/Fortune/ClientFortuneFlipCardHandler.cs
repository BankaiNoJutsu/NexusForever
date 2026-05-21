using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneFlipCardHandler : IMessageHandler<IWorldSession, ClientFortuneFlipCard>
    {
        private readonly ILogger<ClientFortuneFlipCardHandler> log;
        private readonly IFortuneSessionManager fortuneSessionManager;

        public ClientFortuneFlipCardHandler(
            ILogger<ClientFortuneFlipCardHandler> log,
            IFortuneSessionManager fortuneSessionManager)
        {
            this.log                   = log;
            this.fortuneSessionManager = fortuneSessionManager;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneFlipCard flipCard)
        {
            log.LogDebug("ClientFortuneFlipCard: player={Player} cardIndex={CardIndex}",
                session.Player?.Guid, flipCard.SelectedCardIndex);
            fortuneSessionManager.FlipCard(session, flipCard);
        }
    }
}
