using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientPublicEventRequestScoreboardHandler : IMessageHandler<IWorldSession, ClientPublicEventRequestScoreboard>
    {
        private readonly ILogger<ClientPublicEventRequestScoreboardHandler> log;

        public ClientPublicEventRequestScoreboardHandler(ILogger<ClientPublicEventRequestScoreboardHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPublicEventRequestScoreboard requestScoreboard)
        {
            log.LogDebug("ClientPublicEventRequestScoreboard: player={Player} eventId={EventId} subscribe={Subscribe}",
                session.Player?.Guid, requestScoreboard.EventId, requestScoreboard.Subscribe);
        }
    }
}
